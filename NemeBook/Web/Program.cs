using Data;
using Data.Repositories;
using DotNetEnv;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Services.Dtos.Registration;
using Services.Interfaces;
using Services.Interfaces.Chats;
using Services.Interfaces.Classes;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Grades;
using Services.Interfaces.Parents;
using Services.Interfaces.Registration;
using Services.Interfaces.Security;
using Services.Interfaces.Students;
using Services.Interfaces.Subjects;
using Services.Interfaces.Teachers;
using Services.Options;
using Services.Repositories;
using Services.Services.Accounts;
using Services.Services.Auth;
using Services.Services.Chats;
using Services.Services.Classes;
using Services.Services.ClassSubjects;
using Services.Services.Email;
using Services.Services.Grades;
using Services.Services.Notifications;
using Services.Services.Parents;
using Services.Services.Registration;
using Services.Services.Security;
using Services.Services.Students;
using Services.Services.Subjects;
using Services.Services.Teachers;
using Web.Hubs;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".env"));
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

if (IsRunningInContainer() && LooksLikeLocalOnlyConnection(connectionString))
{
    var saPassword = builder.Configuration["MSSQL_SA_PASSWORD"] ?? "Your_strong_password_123!";
    connectionString = $"Server=mssql,1433;Database=NemeBook;User Id=sa;Password={saPassword};TrustServerCertificate=True;Encrypt=False;";
}

builder.Services.AddDbContext<NemeBookDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(180);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

builder.Services.AddTransient<RateLimitingOptions>();

// Register repositories.
builder.Services.AddScoped<IAbsenceRepository, AbsenceRepository>();
builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IClassScheduleEntryRepository, ClassScheduleEntryRepository>();
builder.Services.AddScoped<IClassSubjectRepository, ClassSubjectRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IParentRepository, ParentRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IRegistrationInvitationRepository, RegistrationInvitationRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Grade Repository
builder.Services.AddScoped<IGradeRepository, GradeRepository>();

// Grade Service
builder.Services.AddScoped<IGradeService, GradeService>();

//User Service
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<IClassSubjectService, ClassSubjectService>();

// Register services.
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IEmailService>(serviceProvider => serviceProvider.GetRequiredService<EmailService>());
builder.Services.AddScoped<IRegistrationEmailSender>(serviceProvider => serviceProvider.GetRequiredService<EmailService>());
builder.Services.AddScoped<IRegistrationImportParser, ExcelRegistrationImportParser>();
builder.Services.AddScoped<IInvitationTokenService, InvitationTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IStudentHomeService, StudentHomeService>();

builder.Services.Configure<RegistrationEmailOptions>(
    builder.Configuration.GetSection("RegistrationEmail"));

builder.Services.Configure<SmtpOptions>(options =>
{
    var emailSection = builder.Configuration.GetSection("Email");

    options.Host = emailSection["SmtpHost"] ?? string.Empty;
    options.Port = emailSection.GetValue<int>("SmtpPort");
    options.Username = emailSection["SmtpUsername"] ?? string.Empty;
    options.Password = emailSection["SmtpPassword"] ?? string.Empty;
    options.FromEmail = emailSection["FromEmail"];
    options.FromName = emailSection["FromName"];
});

// Add Cookie Authentication.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddSingleton<BackgroundEmailQueue>();
builder.Services.AddSingleton<IBackgroundEmailQueue>(serviceProvider => serviceProvider.GetRequiredService<BackgroundEmailQueue>());
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<BackgroundEmailQueue>());

var app = builder.Build();

await EnsureDatabaseReadyAndMigratedAsync(app);
await SeedDefaultUsersAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var isRunningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!isRunningInContainer)
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static bool IsRunningInContainer()
{
    return string.Equals(
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
        "true",
        StringComparison.OrdinalIgnoreCase);
}

static bool LooksLikeLocalOnlyConnection(string connectionString)
{
    return connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Server=localhost", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Data Source=localhost", StringComparison.OrdinalIgnoreCase);
}

static async Task EnsureDatabaseReadyAndMigratedAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

    const int maxAttempts = 15;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NemeBookDbContext>();

            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database is ready and migrations are applied.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 15));
            logger.LogWarning(
                ex,
                "Database not ready yet. Retry {Attempt}/{MaxAttempts} in {DelaySeconds} seconds...",
                attempt,
                maxAttempts,
                delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

    using var finalScope = app.Services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<NemeBookDbContext>();
    await finalDbContext.Database.MigrateAsync();
}

static async Task SeedDefaultUsersAsync(WebApplication app)
{
    await SeedUserBySectionAsync(app, "SeedPrincipal", UserRole.Principal, "PrincipalSeeder");
    await SeedUserBySectionAsync(app, "SeedTeacher", UserRole.Teacher, "TeacherSeeder");
    await SeedUserBySectionAsync(app, "SeedParent", UserRole.Parent, "ParentSeeder");
    await SeedUserBySectionAsync(app, "SeedStudent", UserRole.Student, "StudentSeeder");
    await SeedGeorgiGeorgievStudentAsync(app);
}

static async Task SeedUserBySectionAsync(
    WebApplication app,
    string sectionName,
    UserRole role,
    string loggerName)
{
    var firstName = app.Configuration[$"{sectionName}:FirstName"];
    var lastName = app.Configuration[$"{sectionName}:LastName"];
    var email = app.Configuration[$"{sectionName}:Email"];
    var password = app.Configuration[$"{sectionName}:Password"];

    if (string.IsNullOrWhiteSpace(firstName) ||
        string.IsNullOrWhiteSpace(lastName) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var registrationService = scope.ServiceProvider.GetRequiredService<IRegistrationService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(loggerName);

    var result = await registrationService.SeedUserAsync(
        new SeedPrincipalRequest
        {
            FirstName = firstName,
            MiddleName = app.Configuration[$"{sectionName}:MiddleName"],
            LastName = lastName,
            Email = email,
            PhoneNumber = app.Configuration[$"{sectionName}:PhoneNumber"],
            Password = password
        },
        role);
    logger.LogInformation(
        "{Role} seed completed. Created: {Created}, UserId: {UserId}, Email: {Email}",
        role,
        result.Created,
        result.UserId,
        email);
}

static async Task SeedGeorgiGeorgievStudentAsync(WebApplication app)
{
    const string email = "georgi.georgiev.highschool@buditel.bg";
    const string password = "BobE0000";

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NemeBookDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("GeorgiGeorgievStudentSeeder");

    var user = await dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Email == email);
    if (user is null)
    {
        user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Georgi",
            LastName = "Georgiev",
            Email = email,
            Password = passwordHasher.HashPassword(password),
            IsActive = true,
            Role = UserRole.Student
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Georgi Georgiev student seed completed. Created: true, UserId: {UserId}, Email: {Email}", user.Id, email);
        return;
    }

    user.FirstName = "Georgi";
    user.LastName = "Georgiev";
    user.Email = email;
    user.Password = passwordHasher.HashPassword(password);
    user.IsActive = true;
    user.Role = UserRole.Student;
    user.IsDeleted = false;

    await dbContext.SaveChangesAsync();

    logger.LogInformation("Georgi Georgiev student seed completed. Created: false, UserId: {UserId}, Email: {Email}", user.Id, email);
}
