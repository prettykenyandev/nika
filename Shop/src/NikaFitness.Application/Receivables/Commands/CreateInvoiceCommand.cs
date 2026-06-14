using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Receivables;
using NikaFitness.Domain.Settings;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Receivables.Commands;

/// <summary>A single line item supplied when creating an invoice.</summary>
public sealed record InvoiceLineInput(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal? TaxPercent);

public sealed record CreateInvoiceCommand(
    string CustomerName,
    string? CustomerEmail,
    DateOnly IssueDate,
    DateOnly DueDate,
    string? Currency,
    string? Notes,
    Guid? CustomerId,
    IReadOnlyList<InvoiceLineInput> Lines) : IRequest<Guid>;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date cannot be before the issue date.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("An invoice needs at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.TaxPercent).InclusiveBetween(0, 100)
                .When(l => l.TaxPercent.HasValue);
        });
    }
}

public sealed class CreateInvoiceCommandHandler(
    IApplicationDbContext db,
    IDocumentNumberGenerator numbers)
    : IRequestHandler<CreateInvoiceCommand, Guid>
{
    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.CompanySettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? CompanySettings.CreateDefault();

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? settings.Currency
            : request.Currency;
        var defaultTax = settings.DefaultTaxRate;

        var invoiceNumber = await numbers.NextAsync(
            "invoice", settings.InvoiceNumberPrefix, cancellationToken);

        var invoice = Invoice.Create(
            invoiceNumber,
            request.CustomerName,
            request.IssueDate,
            request.DueDate,
            currency,
            request.CustomerEmail,
            request.Notes,
            request.CustomerId);

        foreach (var line in request.Lines)
        {
            var taxRate = line.TaxPercent.HasValue ? new TaxRate(line.TaxPercent.Value) : defaultTax;
            invoice.AddLine(
                line.Description,
                line.Quantity,
                new Money(line.UnitPrice, currency),
                taxRate);
        }

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }
}
