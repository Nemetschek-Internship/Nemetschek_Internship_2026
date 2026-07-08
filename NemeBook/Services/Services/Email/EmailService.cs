using Entities.Enums;
using EmailAttachment = Entities.Models.EmailAttachment;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Services.Dtos.Registration;
using Services.Interfaces;
using Services.Interfaces.Registration;
using Services.Options;

namespace Services.Services.Email;

public class EmailService : IEmailService, IRegistrationEmailSender
{
    private readonly ILogger<EmailService> _logger;
    private readonly RegistrationEmailOptions _registrationEmailOptions;
    private readonly SmtpOptions _smtpOptions;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<RegistrationEmailOptions> registrationEmailOptions,
        IOptions<SmtpOptions> smtpOptions)
    {
        _logger = logger;
        _registrationEmailOptions = registrationEmailOptions.Value;
        _smtpOptions = smtpOptions.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string content, CancellationToken cancellationToken = default)
    {
        await SendEmailInternalAsync(to, subject, content, null, cancellationToken);
    }

    public async Task SendEmailAsync(List<string> recipients, string subject, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending batch email to {RecipientCount} recipients", recipients.Count);
            var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, content, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending batch email");
            throw;
        }
    }

    public async Task SendEmailWithAttachmentsAsync(string to, string subject, string content, List<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        await SendEmailInternalAsync(to, subject, content, attachments, cancellationToken);
    }

    private async Task SendEmailInternalAsync(
        string to,
        string subject,
        string content,
        List<EmailAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_smtpOptions.Host) || string.IsNullOrWhiteSpace(_smtpOptions.Username))
            {
                _logger.LogWarning("SMTP is not configured. Email to {To} was skipped. Content: {Content}", to, content);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpOptions.FromName ?? "NemeBook", _smtpOptions.FromEmail ?? _smtpOptions.Username));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = content };

            if (attachments is { Count: > 0 })
            {
                foreach (var attachment in attachments)
                {
                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.FileContent, ContentType.Parse(attachment.ContentType));
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email successfully sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendNotificationEmailAsync(string recipientEmail, string recipientName, string notificationTitle, string notificationMessage, CancellationToken cancellationToken = default)
    {
        var content = $"<h2>NemeBook Notification</h2><p>Hello {recipientName},</p><p><b>{notificationTitle}</b></p><p>{notificationMessage}</p><p>Please log in to NemeBook for more details.</p>";
        await SendEmailAsync(recipientEmail, $"NemeBook: {notificationTitle}", content, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        var content = $"<h2>Password Reset</h2><p>Hello {recipientName},</p><p>Click the link below to reset your password:</p><p><a href='{resetLink}'>{resetLink}</a></p><p>This link expires in 1 hour.</p>";
        await SendEmailAsync(recipientEmail, "NemeBook: Password Reset Request", content, cancellationToken);
    }

    public async Task SendInvitationAsync(RegistrationEmailRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = request.Type == RegistrationInvitationType.ParentSignUp
            ? "NemeBook: Complete Parent Registration"
            : "NemeBook: Set Your Password";

        var link = BuildRegistrationInvitationLink(request);
        var content = $"<h2>Welcome to NemeBook</h2><p>Please use the link below to complete your registration:</p><p><a href='{link}'>{link}</a></p><p>Expires at: {request.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC</p>";

        await SendEmailAsync(request.Email, subject, content, cancellationToken);
    }

    private string BuildRegistrationInvitationLink(RegistrationEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(_registrationEmailOptions.BaseUrl))
        {
            throw new InvalidOperationException("RegistrationEmail:BaseUrl is not configured.");
        }

        var path = request.Type == RegistrationInvitationType.ParentSignUp
            ? _registrationEmailOptions.ParentSignUpPath
            : _registrationEmailOptions.SetPasswordPath;

        var baseUrl = _registrationEmailOptions.BaseUrl.TrimEnd('/');
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        var separator = normalizedPath.Contains('?') ? '&' : '?';

        return $"{baseUrl}{normalizedPath}{separator}token={Uri.EscapeDataString(request.Token)}";
    }
}