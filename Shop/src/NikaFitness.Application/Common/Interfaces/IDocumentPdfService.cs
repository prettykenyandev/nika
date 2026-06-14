using NikaFitness.Application.Expenses.Dtos;
using NikaFitness.Application.Receivables.Dtos;
using NikaFitness.Application.Settings.Dtos;

namespace NikaFitness.Application.Common.Interfaces;

/// <summary>
/// Renders finance documents (invoices, bills) to branded PDF byte arrays using the
/// company profile for the letterhead. The implementation lives in Infrastructure.
/// </summary>
public interface IDocumentPdfService
{
    byte[] RenderInvoice(InvoiceDetailDto invoice, CompanySettingsDto company);
    byte[] RenderBill(BillDetailDto bill, CompanySettingsDto company);
}
