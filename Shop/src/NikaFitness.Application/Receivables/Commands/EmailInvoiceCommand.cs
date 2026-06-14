using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Receivables.Queries;
using NikaFitness.Application.Settings.Queries;
using NikaFitness.Domain.Common;

namespace NikaFitness.Application.Receivables.Commands;

/// <summary>Renders the invoice to PDF and emails it to the customer (dev sender logs it).</summary>
public sealed record EmailInvoiceCommand(Guid InvoiceId, string? OverrideEmail) : IRequest;

public sealed class EmailInvoiceCommandHandler(
    IApplicationDbContext db,
    ISender sender,
    IDocumentPdfService pdf,
    IEmailSender email)
    : IRequestHandler<EmailInvoiceCommand>
{
    public async Task Handle(EmailInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);

        var to = string.IsNullOrWhiteSpace(request.OverrideEmail)
            ? invoice.CustomerEmail
            : request.OverrideEmail;
        if (string.IsNullOrWhiteSpace(to))
            throw new DomainException("This invoice has no customer email. Add one before emailing.");

        var dto = InvoiceMapping.ToDetail(invoice);
        var company = await sender.Send(new GetCompanySettingsQuery(), cancellationToken);

        var bytes = pdf.RenderInvoice(dto, company);
        var companyName = string.IsNullOrWhiteSpace(company.TradingName)
            ? company.LegalName
            : company.TradingName!;

        var html =
            $"<p>Hi {invoice.CustomerName},</p>" +
            $"<p>Please find attached invoice <strong>{invoice.InvoiceNumber}</strong> " +
            $"for {invoice.Currency} {invoice.AmountDue:N2} due by {invoice.DueDate:dd MMM yyyy}.</p>" +
            $"<p>Thank you,<br/>{companyName}</p>";

        await email.SendAsync(
            to!,
            $"Invoice {invoice.InvoiceNumber} from {companyName}",
            html,
            new[] { new EmailAttachment($"{invoice.InvoiceNumber}.pdf", "application/pdf", bytes) },
            cancellationToken);
    }
}
