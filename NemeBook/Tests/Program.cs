using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Options;
using Services.Services.Email;

string? envPath = null;
var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());

Console.WriteLine($"[DEBUG] Starting search from: {currentDir.FullName}");

while (currentDir != null)
{
    var potentialPath = Path.Combine(currentDir.FullName, ".env");
    Console.WriteLine($"[DEBUG] Checking: {potentialPath}");
    if (File.Exists(potentialPath))
    {
        envPath = potentialPath;
        break;
    }
    currentDir = currentDir.Parent;
}

if (envPath != null)
{
    Console.WriteLine($"[INFO] Found and loading .env from: {envPath}");
    DotNetEnv.Env.Load(envPath);
}
else
{
    Console.WriteLine("[ERROR] .env file was NOT found in any parent directory.");
    Console.WriteLine("[FIX] Create a file named '.env' in your GitHub/Nemetschek_Internship_2026/NemeBook folder.");
}

var smtpUser = Environment.GetEnvironmentVariable("Email__SmtpUsername");
var smtpPass = Environment.GetEnvironmentVariable("Email__SmtpPassword");

if (string.IsNullOrEmpty(smtpUser)) Console.WriteLine("[ERROR] Variable 'Email__SmtpUsername' is empty.");
if (string.IsNullOrEmpty(smtpPass)) Console.WriteLine("[ERROR] Variable 'Email__SmtpPassword' is empty.");

var smtpValues = new SmtpOptions(
    "smtp.gmail.com",
    587,
    smtpUser ?? "placeholder",
    smtpPass ?? "placeholder",
    smtpUser,
    "NemeBook Test Runner"
);

var smtpOptions = Options.Create(smtpValues);
var registrationOptions = Options.Create(new RegistrationEmailOptions { BaseUrl = "http://localhost:5000" });

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<EmailService>();

var emailService = new EmailService(logger, registrationOptions, smtpOptions);

Console.WriteLine($"[TEST] Attempting send via: {smtpValues.Username}");

try
{
    await emailService.SendNotificationEmailAsync(
        "georgi.georgiev.highschool@buditel.bg",
        "Jane Doe",
        "Final Integration Test",
        $"Success! Time: {DateTime.Now}"
    );
    Console.WriteLine("✓ Success! Check your inbox.");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Failure: {ex.Message}");
}