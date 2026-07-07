using System.Linq;
using Entities.Enums;
using Entities.Models;
using EmailAttachment = Entities.Models.EmailAttachment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using Services.Dtos.Registration;
using Services.Interfaces;
using Services.Interfaces.Registration;
using Services.Options;

namespace Services.Services.Email;

public class EmailService : IEmailService, IRegistrationEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly RegistrationEmailOptions _registrationEmailOptions;

    private const string ResendApiKeyKey = "Email:ResendApiKey";
    private const string FromEmailKey = "Email:FromEmail";
    private const string FromNameKey = "Email:FromName";

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IOptions<RegistrationEmailOptions> registrationEmailOptions)
    {
        _configuration = configuration;
        _logger = logger;
        _registrationEmailOptions = registrationEmailOptions.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var resendApiKey = _configuration[ResendApiKeyKey] ?? "re_send_api_key_placeholder";
            var fromEmail = _configuration[FromEmailKey] ?? "noreply@nemebook.com";
            var fromName = _configuration[FromNameKey] ?? "NemeBook";

            var fromAddress = $"{fromName} <{fromEmail}>";

            _logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);

            if (string.IsNullOrWhiteSpace(resendApiKey) || resendApiKey.Contains("placeholder"))
            {
                _logger.LogWarning("Resend API key is not configured properly. Skipping actual send to {To}.", to);
                _logger.LogDebug("Email body: {Content}", content);
                return;
            }

            var resendClient = ResendClient.Create(resendApiKey);
            var message = new EmailMessage
            {
                From = fromAddress,
                To = new[] { to },
                Subject = subject,
                TextBody = content
            };

            await resendClient.EmailSendAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }

    public async Task SendEmailAsync(List<string> recipients, string subject, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email to {RecipientCount} recipients with subject '{Subject}'", recipients.Count, subject);

            var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, content, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Batch email sent successfully to {RecipientCount} recipients", recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending batch email to {RecipientCount} recipients", recipients.Count);
            throw;
        }
    }

    public async Task SendEmailWithAttachmentsAsync(string to, string subject, string content, List<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email with {AttachmentCount} attachments to {To}", attachments.Count, to);

            var resendApiKey = _configuration[ResendApiKeyKey] ?? "re_send_api_key_placeholder";
            var fromEmail = _configuration[FromEmailKey] ?? "noreply@nemebook.com";
            var fromName = _configuration[FromNameKey] ?? "NemeBook";
            var fromAddress = $"{fromName} <{fromEmail}>";

            if (string.IsNullOrWhiteSpace(resendApiKey) || resendApiKey.Contains("placeholder"))
            {
                _logger.LogWarning("Resend API key is not configured properly. Skipping actual send to {To}.", to);
                _logger.LogDebug("Email body: {Content}", content);
                return;
            }

            var resendClient = ResendClient.Create(resendApiKey);
            var message = new EmailMessage
            {
                From = fromAddress,
                To = new[] { to },
                Subject = subject,
                TextBody = content,
                Attachments = attachments.Select(a => new Resend.EmailAttachment
                {
                    Filename = a.FileName,
                    Content = a.FileContent,
                    ContentType = a.ContentType
                }).ToList()
            };

            await resendClient.EmailSendAsync(message, cancellationToken);
            _logger.LogInformation("Email with attachments sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email with attachments to {To}", to);
            throw;
        }
    }

    public async Task SendNotificationEmailAsync(string recipientEmail, string recipientName, string notificationTitle, string notificationMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = GenerateNotificationEmailContent(recipientName, notificationTitle, notificationMessage);
            await SendEmailAsync(recipientEmail, $"NemeBook: {notificationTitle}", content, cancellationToken);

            _logger.LogInformation("Notification email sent to {RecipientEmail}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification email to {RecipientEmail}", recipientEmail);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string resetLink,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = GeneratePasswordResetEmailContent(recipientName, resetLink);
            await SendEmailAsync(recipientEmail, "NemeBook: Password reset request", content, cancellationToken);

            _logger.LogInformation("Password reset email sent to {RecipientEmail}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {RecipientEmail}", recipientEmail);
            throw;
        }
    }

    public async Task SendInvitationAsync(
        RegistrationEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = request.Type == RegistrationInvitationType.ParentSignUp
            ? "NemeBook: Complete parent registration"
            : "NemeBook: Set your password";

        var content = GenerateRegistrationInvitationEmailContent(request);
        await SendEmailAsync(request.Email, subject, content, cancellationToken);

        _logger.LogInformation("Registration invitation email sent to {RecipientEmail}", request.Email);
    }

    private string GeneratePasswordResetEmailContent(string recipientName, string resetLink)
    {
        return $"NemeBook Password Reset\n\nHello {recipientName},\n\n" +
               $"Use the link below to reset your password:\n{resetLink}\n\n" +
               "This link will expire in one hour. If you did not request a password reset, ignore this email.\n\n" +
               "Thanks,\nNemeBook Team";
    }

    private string GenerateNotificationEmailContent(string recipientName, string title, string message)
    {
        return $"NemeBook Notification\n\nHello {recipientName},\n\n" +
               $"{title}\n{message}\n\n" +
               "Please log in to NemeBook to view more details.\n\nThanks,\nNemeBook Team";
    }

    private string GenerateRegistrationInvitationEmailContent(RegistrationEmailRequest request)
    {
        var link = BuildRegistrationInvitationLink(request);
        var actionText = request.Type == RegistrationInvitationType.ParentSignUp
            ? "complete your parent registration"
            : "set your password";

        return $"NemeBook Registration\n\nHello,\n\n" +
               $"Use the link below to {actionText}:\n{link}\n\n" +
               $"This link expires at {request.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC.\n\n" +
               "Thanks,\nNemeBook Team";
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
