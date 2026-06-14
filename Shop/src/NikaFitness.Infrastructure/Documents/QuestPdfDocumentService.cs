using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Expenses.Dtos;
using NikaFitness.Application.Receivables.Dtos;
using NikaFitness.Application.Settings.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NikaFitness.Infrastructure.Documents;

/// <summary>Renders branded invoice/bill PDFs with QuestPDF.</summary>
public sealed class QuestPdfDocumentService : IDocumentPdfService
{
    static QuestPdfDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] RenderInvoice(InvoiceDetailDto invoice, CompanySettingsDto company)
    {
        var rows = invoice.Lines
            .Select(l => new DocLine(l.Description, l.Quantity, l.UnitPrice, l.TaxPercent, l.LineTotal))
            .ToList();

        return Render(
            company,
            title: "INVOICE",
            number: invoice.InvoiceNumber,
            status: invoice.Status,
            partyLabel: "Bill to",
            partyName: invoice.CustomerName,
            partyContact: invoice.CustomerEmail,
            issueDate: invoice.IssueDate,
            dueDate: invoice.DueDate,
            currency: invoice.Currency,
            rows: rows,
            subtotal: invoice.Subtotal,
            taxTotal: invoice.TaxTotal,
            total: invoice.Total,
            amountPaid: invoice.AmountPaid,
            amountDue: invoice.AmountDue,
            notes: invoice.Notes,
            footer: company.InvoiceFooter,
            paymentInstructions: company.PaymentInstructions);
    }

    public byte[] RenderBill(BillDetailDto bill, CompanySettingsDto company)
    {
        var rows = bill.Lines
            .Select(l => new DocLine(l.Description, l.Quantity, l.UnitCost, l.TaxPercent, l.LineTotal))
            .ToList();

        return Render(
            company,
            title: "BILL",
            number: bill.BillNumber,
            status: bill.Status,
            partyLabel: "From vendor",
            partyName: bill.VendorName,
            partyContact: bill.SupplierReference is null ? null : $"Ref {bill.SupplierReference}",
            issueDate: bill.IssueDate,
            dueDate: bill.DueDate,
            currency: bill.Currency,
            rows: rows,
            subtotal: bill.Subtotal,
            taxTotal: bill.TaxTotal,
            total: bill.Total,
            amountPaid: bill.AmountPaid,
            amountDue: bill.AmountDue,
            notes: bill.Notes,
            footer: company.InvoiceFooter,
            paymentInstructions: null);
    }

    private sealed record DocLine(
        string Description, decimal Quantity, decimal UnitPrice, decimal TaxPercent, decimal LineTotal);

    private static byte[] Render(
        CompanySettingsDto company,
        string title,
        string number,
        string status,
        string partyLabel,
        string partyName,
        string? partyContact,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency,
        IReadOnlyList<DocLine> rows,
        decimal subtotal,
        decimal taxTotal,
        decimal total,
        decimal amountPaid,
        decimal amountDue,
        string? notes,
        string? footer,
        string? paymentInstructions)
    {
        var companyName = string.IsNullOrWhiteSpace(company.TradingName)
            ? company.LegalName
            : company.TradingName;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1f2937"));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName).FontSize(18).Bold();
                            if (!string.IsNullOrWhiteSpace(company.LegalName) &&
                                !string.Equals(company.LegalName, companyName, StringComparison.OrdinalIgnoreCase))
                                col.Item().Text(company.LegalName).FontColor("#6b7280");
                            foreach (var line in CompanyAddressLines(company))
                                col.Item().Text(line).FontColor("#6b7280");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text(title).FontSize(22).Bold().FontColor("#111827");
                            col.Item().AlignRight().Text(number).FontSize(12).FontColor("#6b7280");
                            col.Item().AlignRight().Text(status).FontColor("#6b7280");
                        });
                    });
                    header.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e5e7eb");
                });

                page.Content().PaddingVertical(14).Column(content =>
                {
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(partyLabel).Bold();
                            col.Item().Text(partyName);
                            if (!string.IsNullOrWhiteSpace(partyContact))
                                col.Item().Text(partyContact!).FontColor("#6b7280");
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text($"Issued: {issueDate:dd MMM yyyy}");
                            col.Item().AlignRight().Text($"Due: {dueDate:dd MMM yyyy}");
                        });
                    });

                    content.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background("#f3f4f6").Padding(6).Text("Description").Bold();
                            h.Cell().Background("#f3f4f6").Padding(6).AlignRight().Text("Qty").Bold();
                            h.Cell().Background("#f3f4f6").Padding(6).AlignRight().Text("Unit").Bold();
                            h.Cell().Background("#f3f4f6").Padding(6).AlignRight().Text("Tax").Bold();
                            h.Cell().Background("#f3f4f6").Padding(6).AlignRight().Text("Amount").Bold();
                        });

                        foreach (var line in rows)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).Text(line.Description);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).AlignRight().Text($"{line.Quantity:0.##}");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).AlignRight().Text(Fmt(line.UnitPrice, currency));
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).AlignRight().Text($"{line.TaxPercent:0.##}%");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).AlignRight().Text(Fmt(line.LineTotal, currency));
                        }
                    });

                    content.Item().PaddingTop(10).AlignRight().Width(220).Column(totals =>
                    {
                        TotalRow(totals, "Subtotal", Fmt(subtotal, currency), false);
                        TotalRow(totals, "Tax", Fmt(taxTotal, currency), false);
                        TotalRow(totals, "Total", Fmt(total, currency), true);
                        TotalRow(totals, "Paid", Fmt(amountPaid, currency), false);
                        TotalRow(totals, "Amount due", Fmt(amountDue, currency), true);
                    });

                    if (!string.IsNullOrWhiteSpace(notes))
                        content.Item().PaddingTop(14).Column(col =>
                        {
                            col.Item().Text("Notes").Bold();
                            col.Item().Text(notes!).FontColor("#374151");
                        });

                    if (!string.IsNullOrWhiteSpace(paymentInstructions))
                        content.Item().PaddingTop(10).Column(col =>
                        {
                            col.Item().Text("Payment instructions").Bold();
                            col.Item().Text(paymentInstructions!).FontColor("#374151");
                        });
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    var footerText = string.IsNullOrWhiteSpace(footer)
                        ? $"{companyName}"
                        : footer!;
                    col.Item().PaddingTop(6).AlignCenter().Text(footerText).FontSize(9).FontColor("#9ca3af");
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void TotalRow(ColumnDescriptor col, string label, string value, bool strong)
    {
        col.Item().Row(row =>
        {
            var l = row.RelativeItem().Text(label);
            var v = row.RelativeItem().AlignRight().Text(value);
            if (strong)
            {
                l.Bold();
                v.Bold();
            }
            else
            {
                l.FontColor("#6b7280");
                v.FontColor("#6b7280");
            }
        });
    }

    private static IEnumerable<string> CompanyAddressLines(CompanySettingsDto c)
    {
        if (!string.IsNullOrWhiteSpace(c.AddressLine1)) yield return c.AddressLine1!;
        if (!string.IsNullOrWhiteSpace(c.AddressLine2)) yield return c.AddressLine2!;
        var cityCountry = string.Join(", ",
            new[] { c.City, c.Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(cityCountry)) yield return cityCountry;
        if (!string.IsNullOrWhiteSpace(c.Email)) yield return c.Email!;
        if (!string.IsNullOrWhiteSpace(c.Phone)) yield return c.Phone!;
        if (!string.IsNullOrWhiteSpace(c.TaxIdentifier)) yield return $"PIN: {c.TaxIdentifier}";
    }

    private static string Fmt(decimal amount, string currency) => $"{currency} {amount:N2}";
}
