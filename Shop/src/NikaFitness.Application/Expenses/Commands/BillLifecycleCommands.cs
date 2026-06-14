using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Expenses.Commands;

public sealed record ApproveBillCommand(Guid BillId) : IRequest;

public sealed class ApproveBillCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ApproveBillCommand>
{
    public async Task Handle(ApproveBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await db.Bills
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken)
            ?? throw new NotFoundException("Bill", request.BillId);

        bill.Approve();
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record CancelBillCommand(Guid BillId) : IRequest;

public sealed class CancelBillCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CancelBillCommand>
{
    public async Task Handle(CancelBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await db.Bills
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken)
            ?? throw new NotFoundException("Bill", request.BillId);

        bill.Cancel();
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RecordBillPaymentCommand(
    Guid BillId,
    decimal Amount,
    DateOnly PaidOn,
    string Method,
    string? Reference) : IRequest;

public sealed class RecordBillPaymentCommandValidator
    : AbstractValidator<RecordBillPaymentCommand>
{
    public RecordBillPaymentCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}

public sealed class RecordBillPaymentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RecordBillPaymentCommand>
{
    public async Task Handle(RecordBillPaymentCommand request, CancellationToken cancellationToken)
    {
        var bill = await db.Bills
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken)
            ?? throw new NotFoundException("Bill", request.BillId);

        bill.RecordPayment(
            new Money(request.Amount, bill.Currency),
            request.PaidOn,
            request.Method,
            request.Reference);

        await db.SaveChangesAsync(cancellationToken);
    }
}
