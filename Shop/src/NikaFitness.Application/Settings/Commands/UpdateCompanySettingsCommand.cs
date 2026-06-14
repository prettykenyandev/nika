using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Settings;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Settings.Commands;

public sealed record UpdateCompanySettingsCommand(
    string LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    string? TaxIdentifier,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Country,
    string? LogoUrl,
    string Currency,
    decimal DefaultTaxPercent,
    string InvoiceNumberPrefix,
    string BillNumberPrefix,
    string PurchaseOrderNumberPrefix,
    string? InvoiceFooter,
    string? PaymentInstructions) : IRequest;

public sealed class UpdateCompanySettingsCommandValidator
    : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TradingName).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(256)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.TaxIdentifier).MaximumLength(60);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.DefaultTaxPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.InvoiceNumberPrefix).NotEmpty().MaximumLength(10);
        RuleFor(x => x.BillNumberPrefix).NotEmpty().MaximumLength(10);
        RuleFor(x => x.PurchaseOrderNumberPrefix).NotEmpty().MaximumLength(10);
    }
}

public sealed class UpdateCompanySettingsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateCompanySettingsCommand>
{
    public async Task Handle(UpdateCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = CompanySettings.CreateDefault();
            db.CompanySettings.Add(settings);
        }

        settings.Update(
            request.LegalName,
            request.TradingName,
            request.Email,
            request.Phone,
            request.TaxIdentifier,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Country,
            request.LogoUrl,
            request.Currency,
            new TaxRate(request.DefaultTaxPercent),
            request.InvoiceNumberPrefix,
            request.BillNumberPrefix,
            request.PurchaseOrderNumberPrefix,
            request.InvoiceFooter,
            request.PaymentInstructions);

        await db.SaveChangesAsync(cancellationToken);
    }
}
