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
        var content = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Your Password</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', 'Cantarell', sans-serif;
            background-color: #f9fafb;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
        }}
        .header {{
            background: linear-gradient(135deg, #F97316, #EA580C);
            padding: 40px 20px;
            text-align: center;
        }}
        .logo {{
            display: inline-block;
            background: rgba(255, 255, 255, 0.2);
            border-radius: 12px;
            padding: 12px;
            margin-bottom: 16px;
        }}
        .logo-icon {{
            width: 48px;
            height: 48px;
            background: white;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: 700;
            color: #F97316;
            font-size: 24px;
        }}
        .header h1 {{
            color: #ffffff;
            margin: 16px 0 0 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 32px;
            color: #1f2937;
            font-size: 16px;
            line-height: 1.6;
        }}
        .greeting {{
            font-size: 18px;
            font-weight: 600;
            margin-bottom: 16px;
            color: #111827;
        }}
        .description {{
            color: #6b7280;
            margin-bottom: 32px;
            font-size: 15px;
        }}
        .button-container {{
            text-align: center;
            margin: 32px 0;
        }}
        .reset-button {{
            display: inline-block;
            background: linear-gradient(135deg, #F97316, #EA580C);
            color: white;
            text-decoration: none;
            padding: 14px 32px;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            transition: all 0.3s ease;
            box-shadow: 0 4px 12px rgba(249, 115, 22, 0.3);
        }}
        .reset-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 16px rgba(249, 115, 22, 0.4);
        }}
        .fallback-link {{
            color: #6b7280;
            font-size: 13px;
            word-break: break-all;
            margin-top: 16px;
        }}
        .fallback-link a {{
            color: #f97316;
            text-decoration: none;
        }}
        .expiration-notice {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 16px;
            border-radius: 4px;
            margin: 24px 0;
            font-size: 14px;
            color: #92400e;
        }}
        .footer {{
            background-color: #f3f4f6;
            padding: 20px;
            text-align: center;
            color: #6b7280;
            font-size: 13px;
            border-top: 1px solid #e5e7eb;
        }}
        .footer-text {{
            margin: 8px 0;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div class=""logo"">
                <div class=""logo-icon"">📚</div>
            </div>
            <h1>NemeBook</h1>
        </div>
        
        <div class=""content"">
            <p class=""greeting"">Hello {recipientName},</p>
            
            <p class=""description"">
                We received a request to reset your password. Click the button below to create a new password for your NemeBook account.
            </p>
            
            <div class=""button-container"">
                <a href=""{resetLink}"" class=""reset-button"">Reset Password</a>
            </div>
            
            <div class=""expiration-notice"">
                <strong>⏱️ Important:</strong> This link will expire in <strong>30 minutes</strong>. If you didn't request a password reset, please ignore this email.
            </div>
            
            <p style=""color: #6b7280; font-size: 14px; margin-top: 24px;"">
                If the button above doesn't work, copy and paste this link into your browser:
            </p>
            <p class=""fallback-link"">
                <a href=""{resetLink}"">{resetLink}</a>
            </p>
        </div>
        
        <div class=""footer"">
            <p class=""footer-text""><strong>NemeBook</strong> — Your Learning Management System</p>
            <p class=""footer-text"">© {DateTime.Now.Year} NemeBook. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        
        await SendEmailAsync(recipientEmail, "NemeBook: Reset Your Password", content, cancellationToken);
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
