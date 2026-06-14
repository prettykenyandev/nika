using NikaFitness.Application.Purchasing.Dtos;
using NikaFitness.Domain.Purchasing;

namespace NikaFitness.Application.Purchasing.Queries;

/// <summary>Maps vendor and purchase-order aggregates to their read DTOs.</summary>
internal static class PurchasingMapping
{
    public static VendorSummaryDto ToSummary(Vendor vendor) => new(
        vendor.Id,
        vendor.Name,
        vendor.ContactName,
        vendor.Email,
        vendor.Phone,
        vendor.PaymentTermDays,
        vendor.IsActive);

    public static VendorDetailDto ToDetail(Vendor vendor) => new(
        vendor.Id,
        vendor.Name,
        vendor.ContactName,
        vendor.Email,
        vendor.Phone,
        vendor.AddressLine1,
        vendor.City,
        vendor.Country,
        vendor.TaxIdentifier,
        vendor.PaymentTermDays,
        vendor.Notes,
        vendor.IsActive);

    public static PurchaseOrderSummaryDto ToSummary(PurchaseOrder po) => new(
        po.Id,
        po.PoNumber,
        po.VendorId,
        po.VendorName,
        po.OrderDate,
        po.ExpectedDate,
        po.Currency,
        po.Status.ToString(),
        po.Total.Amount);

    public static PurchaseOrderDetailDto ToDetail(PurchaseOrder po)
    {
        var lines = po.Lines.Select(l => new PurchaseOrderLineDto(
            l.Id,
            l.ProductVariantId,
            l.Description,
            l.Sku,
            l.Quantity,
            l.QuantityReceived,
            l.QuantityOutstanding,
            l.UnitCost.Amount,
            l.TaxRate.Percent,
            l.LineNet.Amount,
            l.LineTax.Amount,
            l.LineTotal.Amount)).ToList();

        return new PurchaseOrderDetailDto(
            po.Id,
            po.PoNumber,
            po.VendorId,
            po.VendorName,
            po.OrderDate,
            po.ExpectedDate,
            po.Currency,
            po.Status.ToString(),
            po.Notes,
            po.GeneratedBillId,
            po.Subtotal.Amount,
            po.TaxTotal.Amount,
            po.Total.Amount,
            lines);
    }
}
