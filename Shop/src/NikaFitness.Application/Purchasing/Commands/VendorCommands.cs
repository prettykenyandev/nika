using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Purchasing;

namespace NikaFitness.Application.Purchasing.Commands;

public sealed record VendorInput(
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? City,
    string? Country,
    string? TaxIdentifier,
    int PaymentTermDays,
    string? Notes);

public sealed class VendorInputValidator : AbstractValidator<VendorInput>
{
    public VendorInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed record CreateVendorCommand(VendorInput Vendor) : IRequest<Guid>;

public sealed class CreateVendorCommandValidator : AbstractValidator<CreateVendorCommand>
{
    public CreateVendorCommandValidator() =>
        RuleFor(x => x.Vendor).SetValidator(new VendorInputValidator());
}

public sealed class CreateVendorCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateVendorCommand, Guid>
{
    public async Task<Guid> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var v = request.Vendor;
        var vendor = Vendor.Create(
            v.Name, v.ContactName, v.Email, v.Phone, v.AddressLine1,
            v.City, v.Country, v.TaxIdentifier, v.PaymentTermDays, v.Notes);

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(cancellationToken);
        return vendor.Id;
    }
}

public sealed record UpdateVendorCommand(Guid Id, VendorInput Vendor) : IRequest;

public sealed class UpdateVendorCommandValidator : AbstractValidator<UpdateVendorCommand>
{
    public UpdateVendorCommandValidator() =>
        RuleFor(x => x.Vendor).SetValidator(new VendorInputValidator());
}

public sealed class UpdateVendorCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateVendorCommand>
{
    public async Task Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Vendor", request.Id);

        var v = request.Vendor;
        vendor.UpdateDetails(
            v.Name, v.ContactName, v.Email, v.Phone, v.AddressLine1,
            v.City, v.Country, v.TaxIdentifier, v.PaymentTermDays, v.Notes);

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record SetVendorActiveCommand(Guid Id, bool IsActive) : IRequest;

public sealed class SetVendorActiveCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetVendorActiveCommand>
{
    public async Task Handle(SetVendorActiveCommand request, CancellationToken cancellationToken)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Vendor", request.Id);

        if (request.IsActive) vendor.Activate();
        else vendor.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }
}
