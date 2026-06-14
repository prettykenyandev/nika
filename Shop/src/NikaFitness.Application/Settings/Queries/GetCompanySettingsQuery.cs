using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Settings.Dtos;
using NikaFitness.Domain.Settings;

namespace NikaFitness.Application.Settings.Queries;

public sealed record GetCompanySettingsQuery : IRequest<CompanySettingsDto>;

public sealed class GetCompanySettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCompanySettingsQuery, CompanySettingsDto>
{
    public async Task<CompanySettingsDto> Handle(
        GetCompanySettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await db.CompanySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        // Fall back to in-memory defaults if the row has not been seeded yet.
        settings ??= CompanySettings.CreateDefault();

        return new CompanySettingsDto(
            settings.LegalName,
            settings.TradingName,
            settings.Email,
            settings.Phone,
            settings.TaxIdentifier,
            settings.AddressLine1,
            settings.AddressLine2,
            settings.City,
            settings.Country,
            settings.LogoUrl,
            settings.Currency,
            settings.DefaultTaxRate.Percent,
            settings.InvoiceNumberPrefix,
            settings.BillNumberPrefix,
            settings.PurchaseOrderNumberPrefix,
            settings.InvoiceFooter,
            settings.PaymentInstructions);
    }
}
