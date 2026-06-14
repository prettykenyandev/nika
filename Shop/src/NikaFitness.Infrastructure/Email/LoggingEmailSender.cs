using Microsoft.Extensions.Logging;
using NikaFitness.Application.Common.Interfaces;

namespace NikaFitness.Infrastructure.Email;

/// <summary>
/// Development email sender that logs the message instead of dispatching it. Swap in a
/// real SMTP/API-backed implementation for production by re-registering <see cref="IEmailSender"/>.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var attachmentInfo = attachments is { Count: > 0 }
            ? string.Join(", ", attachments.Select(a => $"{a.FileName} ({a.Content.Length} bytes)"))
            : "none";

        logger.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject} | Attachments: {Attachments}",
            to, subject, attachmentInfo);

        return Task.CompletedTask;
    }
}
