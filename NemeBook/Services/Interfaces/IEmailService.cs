using Entities.Models;

namespace Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient
    /// </summary>
    Task SendEmailAsync(string to, string subject, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    Task SendEmailAsync(List<string> recipients, string subject, string content, CancellationToken cancellationToken = default);

    /*
    /// <summary>
    /// Sends an email with attachments
    /// </summary>
    Task SendEmailWithAttachmentsAsync(string to, string subject, string content, List<EmailAttachment> attachments, CancellationToken cancellationToken = default);
    */
    
    /// <summary>
    /// Sends a templated email notification
    /// </summary>
    Task SendNotificationEmailAsync(string recipientEmail, string recipientName, string notificationTitle, string notificationMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email that contains a link the user can use to reset their password.
    /// </summary>
    Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default);

}
