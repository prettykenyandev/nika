namespace NikaFitness.Application.Common.Interfaces;

/// <summary>An email attachment (e.g. a rendered invoice PDF).</summary>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Sends transactional email (invoice delivery, receipts). The dev implementation logs
/// instead of sending; production can swap in a real SMTP/API provider.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
