using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Receivables;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Receivables.Commands;

public sealed record SendInvoiceCommand(Guid InvoiceId) : IRequest;

public sealed class SendInvoiceCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SendInvoiceCommand>
{
    public async Task Handle(SendInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);

        invoice.Send();
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record VoidInvoiceCommand(Guid InvoiceId) : IRequest;

public sealed class VoidInvoiceCommandHandler(IApplicationDbContext db)
    : IRequestHandler<VoidInvoiceCommand>
{
    public async Task Handle(VoidInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);

        invoice.Void();
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RecordInvoicePaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    DateOnly ReceivedOn,
    string Method,
    string? Reference) : IRequest;

public sealed class RecordInvoicePaymentCommandValidator
    : AbstractValidator<RecordInvoicePaymentCommand>
{
    public RecordInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}

public sealed class RecordInvoicePaymentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RecordInvoicePaymentCommand>
{
    public async Task Handle(RecordInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);

        invoice.RecordPayment(
            new Money(request.Amount, invoice.Currency),
            request.ReceivedOn,
            request.Method,
            request.Reference);

        await db.SaveChangesAsync(cancellationToken);
    }
}
