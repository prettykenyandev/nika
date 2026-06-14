using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Customers.Dtos;
using NikaFitness.Domain.Customers;

namespace NikaFitness.Application.Customers.Commands;

public sealed class CustomerInputValidator : AbstractValidator<CustomerInput>
{
    public CustomerInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed record CreateCustomerCommand(CustomerInput Customer) : IRequest<Guid>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator() =>
        RuleFor(x => x.Customer).SetValidator(new CustomerInputValidator());
}

public sealed class CreateCustomerCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var c = request.Customer;
        var customer = Customer.Create(c.Name, c.Email, c.Phone, c.AddressLine1, c.City, c.Country, c.Notes);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}

public sealed record UpdateCustomerCommand(Guid Id, CustomerInput Customer) : IRequest;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator() =>
        RuleFor(x => x.Customer).SetValidator(new CustomerInputValidator());
}

public sealed class UpdateCustomerCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Customer", request.Id);

        var c = request.Customer;
        customer.UpdateDetails(c.Name, c.Email, c.Phone, c.AddressLine1, c.City, c.Country, c.Notes);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record SetCustomerActiveCommand(Guid Id, bool IsActive) : IRequest;

public sealed class SetCustomerActiveCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetCustomerActiveCommand>
{
    public async Task Handle(SetCustomerActiveCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Customer", request.Id);

        if (request.IsActive) customer.Activate();
        else customer.Deactivate();
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Creates customer records from distinct emails on existing orders and links those orders to
/// the new (or already-existing) customer. Returns how many customers were created and orders linked.
/// </summary>
public sealed record BackfillCustomersFromOrdersCommand : IRequest<BackfillResult>;

public sealed record BackfillResult(int CustomersCreated, int OrdersLinked);

public sealed class BackfillCustomersFromOrdersCommandHandler(IApplicationDbContext db)
    : IRequestHandler<BackfillCustomersFromOrdersCommand, BackfillResult>
{
    public async Task<BackfillResult> Handle(
        BackfillCustomersFromOrdersCommand request,
        CancellationToken cancellationToken)
    {
        var orders = await db.Orders.ToListAsync(cancellationToken);

        var existing = await db.Customers
            .Where(c => c.Email != null)
            .ToListAsync(cancellationToken);
        var byEmail = existing
            .Where(c => c.Email is not null)
            .ToDictionary(c => c.Email!, c => c, StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var linked = 0;

        foreach (var group in orders
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerEmail))
            .GroupBy(o => o.CustomerEmail, StringComparer.OrdinalIgnoreCase))
        {
            var email = group.Key;
            if (!byEmail.TryGetValue(email, out var customer))
            {
                var displayName = group
                    .Select(o => o.ShippingAddress?.FullName)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? email;

                customer = Customer.Create(displayName, email);
                db.Customers.Add(customer);
                byEmail[email] = customer;
                created++;
            }

            foreach (var order in group.Where(o => o.CustomerId != customer.Id))
            {
                order.AssignCustomer(customer.Id);
                linked++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new BackfillResult(created, linked);
    }
}
