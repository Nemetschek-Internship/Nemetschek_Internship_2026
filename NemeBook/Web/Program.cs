using Data;
using Data.Repositories;
using DotNetEnv;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Services.Dtos.Registration;
using Services.Interfaces;
using Services.Interfaces.Chats;
using Services.Interfaces.Classes;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Grades;
using Services.Interfaces.News;
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
using Services.Services.News;
using Services.Services.Parents;
using Services.Services.Registration;
using Services.Services.Security;
using Services.Services.Students;
using Services.Services.Subjects;
using Services.Services.Teachers;
using Web.Hubs;
using Web.Services;
using Web.Services.Admin;

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
builder.Services.AddScoped<INewsRepository, NewsRepository>();
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
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<INotificationPushService, SignalRNotificationPushService>();
builder.Services.AddScoped<IStudentHomeService, StudentHomeService>();
builder.Services.AddScoped<ITeacherHomeService, TeacherHomeService>();
builder.Services.AddScoped<IPrincipalDashboardService, PrincipalDashboardService>();
builder.Services.AddScoped<IPrincipalClassesService, PrincipalClassesService>();
builder.Services.AddScoped<IPrincipalClassManagementService, PrincipalClassManagementService>();
builder.Services.AddScoped<IPrincipalReportsService, PrincipalReportsService>();

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
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Insert(0, "/Views/Admin/{1}/{0}.cshtml");
        options.ViewLocationFormats.Insert(1, "/Views/Admin/Shared/{0}.cshtml");
    });
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

app.MapHub<NotificationHub>("/hubs/notifications");

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
    await SeedDemoDataAsync(app);
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
    user.Role = UserRole.Student;
    user.IsDeleted = false;
    user.IsActive = true;

    await dbContext.SaveChangesAsync();

    logger.LogInformation("Georgi Georgiev student seed completed. Created: false, UserId: {UserId}, Email: {Email}", user.Id, email);
}

static async Task SeedDemoDataAsync(WebApplication app)
{
    const string principalEmail = "admin@nemebook.local";
    const string principalPassword = "Admin123!";
    const string primaryTeacherEmail = "teacher@nemebook.local";
    const string primaryTeacherPassword = "Teacher123!";
    const string studentEmail = "student@nemebook.local";
    const string studentPassword = "Student123!";
    const string parentEmail = "parent@nemebook.local";
    const string parentPassword = "Parent123!";

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NemeBookDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");

    var principalUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        principalEmail,
        principalPassword,
        "Антония",
        null,
        "Димитрова",
        UserRole.Principal);
    var primaryTeacherUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        primaryTeacherEmail,
        primaryTeacherPassword,
        "Мария",
        null,
        "Иванова",
        UserRole.Teacher);
    var mathTeacherUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        "math.teacher@nemebook.local",
        "Teacher123!",
        "Петър",
        null,
        "Петров",
        UserRole.Teacher);
    var englishTeacherUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        "english.teacher@nemebook.local",
        "Teacher123!",
        "Елена",
        null,
        "Николова",
        UserRole.Teacher);
    var studentUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        studentEmail,
        studentPassword,
        "Николай",
        null,
        "Димитров",
        UserRole.Student);
    var secondStudentUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        "elena.student@nemebook.local",
        "Student123!",
        "Елена",
        null,
        "Стоянова",
        UserRole.Student);
    var thirdStudentUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        "martin.student@nemebook.local",
        "Student123!",
        "Мартин",
        null,
        "Георгиев",
        UserRole.Student);
    var fourthStudentUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        "victoria.student@nemebook.local",
        "Student123!",
        "Виктория",
        null,
        "Тодорова",
        UserRole.Student);
    var parentUser = await UpsertDemoUserAsync(
        dbContext,
        passwordHasher,
        parentEmail,
        parentPassword,
        "Иван",
        null,
        "Димитров",
        UserRole.Parent);

    await dbContext.SaveChangesAsync();

    var primaryTeacher = await UpsertDemoTeacherAsync(dbContext, primaryTeacherUser, new DateOnly(1986, 4, 12));
    var mathTeacher = await UpsertDemoTeacherAsync(dbContext, mathTeacherUser, new DateOnly(1981, 9, 24));
    var englishTeacher = await UpsertDemoTeacherAsync(dbContext, englishTeacherUser, new DateOnly(1990, 2, 6));

    var history = await UpsertDemoSubjectAsync(dbContext, "История");
    var math = await UpsertDemoSubjectAsync(dbContext, "Математика");
    var bulgarian = await UpsertDemoSubjectAsync(dbContext, "Български език и литература");
    var informationTechnology = await UpsertDemoSubjectAsync(dbContext, "Информационни технологии");
    var english = await UpsertDemoSubjectAsync(dbContext, "Английски език");
    var biology = await UpsertDemoSubjectAsync(dbContext, "Биология");

    await dbContext.SaveChangesAsync();

    await UpsertDemoTeacherSubjectAsync(dbContext, primaryTeacher, history);
    await UpsertDemoTeacherSubjectAsync(dbContext, primaryTeacher, bulgarian);
    await UpsertDemoTeacherSubjectAsync(dbContext, mathTeacher, math);
    await UpsertDemoTeacherSubjectAsync(dbContext, mathTeacher, informationTechnology);
    await UpsertDemoTeacherSubjectAsync(dbContext, englishTeacher, english);
    await UpsertDemoTeacherSubjectAsync(dbContext, englishTeacher, biology);

    var classOneB = await UpsertDemoClassAsync(dbContext, 1, 'Б', primaryTeacher);
    var classTwoA = await UpsertDemoClassAsync(dbContext, 2, 'А', mathTeacher);

    await dbContext.SaveChangesAsync();

    var demoStudent = await UpsertDemoStudentAsync(dbContext, studentUser, classOneB, new DateOnly(2018, 5, 8));
    var secondStudent = await UpsertDemoStudentAsync(dbContext, secondStudentUser, classOneB, new DateOnly(2018, 3, 18));
    var thirdStudent = await UpsertDemoStudentAsync(dbContext, thirdStudentUser, classOneB, new DateOnly(2017, 11, 2));
    var fourthStudent = await UpsertDemoStudentAsync(dbContext, fourthStudentUser, classOneB, new DateOnly(2018, 1, 27));
    var parent = await UpsertDemoParentAsync(dbContext, parentUser, demoStudent);

    _ = parent;
    _ = secondStudent;
    _ = thirdStudent;
    _ = fourthStudent;

    var oneBHistory = await UpsertDemoClassSubjectAsync(dbContext, classOneB, history, primaryTeacher);
    var oneBMath = await UpsertDemoClassSubjectAsync(dbContext, classOneB, math, mathTeacher);
    var oneBBulgarian = await UpsertDemoClassSubjectAsync(dbContext, classOneB, bulgarian, primaryTeacher);
    var oneBIt = await UpsertDemoClassSubjectAsync(dbContext, classOneB, informationTechnology, mathTeacher);
    var oneBEnglish = await UpsertDemoClassSubjectAsync(dbContext, classOneB, english, englishTeacher);
    var oneBBiology = await UpsertDemoClassSubjectAsync(dbContext, classOneB, biology, englishTeacher);
    var twoAMath = await UpsertDemoClassSubjectAsync(dbContext, classTwoA, math, mathTeacher);
    var twoAHistory = await UpsertDemoClassSubjectAsync(dbContext, classTwoA, history, primaryTeacher);
    var twoAEnglish = await UpsertDemoClassSubjectAsync(dbContext, classTwoA, english, englishTeacher);

    await dbContext.SaveChangesAsync();

    var scheduleEntries = new[]
    {
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBHistory, DayOfWeek.Monday, 1, "08:00", "08:40"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBMath, DayOfWeek.Monday, 2, "08:50", "09:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBBulgarian, DayOfWeek.Monday, 3, "09:50", "10:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBHistory, DayOfWeek.Tuesday, 1, "08:00", "08:40"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBIt, DayOfWeek.Tuesday, 2, "08:50", "09:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBEnglish, DayOfWeek.Tuesday, 3, "09:50", "10:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBMath, DayOfWeek.Wednesday, 1, "08:00", "08:40"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBBiology, DayOfWeek.Wednesday, 2, "08:50", "09:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBBulgarian, DayOfWeek.Thursday, 1, "08:00", "08:40"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBEnglish, DayOfWeek.Thursday, 2, "08:50", "09:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBMath, DayOfWeek.Friday, 1, "08:00", "08:40"),
        await UpsertDemoScheduleEntryAsync(dbContext, classOneB, oneBIt, DayOfWeek.Friday, 2, "08:50", "09:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classTwoA, twoAMath, DayOfWeek.Monday, 1, "08:00", "08:40"),
        await UpsertDemoScheduleEntryAsync(dbContext, classTwoA, twoAHistory, DayOfWeek.Tuesday, 2, "08:50", "09:30"),
        await UpsertDemoScheduleEntryAsync(dbContext, classTwoA, twoAEnglish, DayOfWeek.Wednesday, 3, "09:50", "10:30")
    };

    await dbContext.SaveChangesAsync();

    var today = DateTime.Today;
    var grade1 = await AddDemoGradeIfMissingAsync(dbContext, demoStudent, oneBHistory, 6m, GradeType.OralExamination, "Отлично представяне по урока за Българското възраждане.", today.AddDays(-2));
    var grade2 = await AddDemoGradeIfMissingAsync(dbContext, demoStudent, oneBMath, 5m, GradeType.Test, "Стабилен резултат на теста за събиране и изваждане.", today.AddDays(-5));
    var grade3 = await AddDemoGradeIfMissingAsync(dbContext, demoStudent, oneBBulgarian, 6m, GradeType.Homework, "Домашната работа е предадена навреме и е много добре оформена.", today.AddDays(-8));
    var grade4 = await AddDemoGradeIfMissingAsync(dbContext, demoStudent, oneBIt, 5m, GradeType.PracticalExamination, "Работи уверено с учебния софтуер.", today.AddDays(-11));
    await AddDemoGradeIfMissingAsync(dbContext, secondStudent, oneBHistory, 5m, GradeType.OralExamination, "Активно участие в час.", today.AddDays(-3));
    await AddDemoGradeIfMissingAsync(dbContext, thirdStudent, oneBMath, 4m, GradeType.Test, "Има нужда от още упражнения.", today.AddDays(-6));
    await AddDemoGradeIfMissingAsync(dbContext, fourthStudent, oneBEnglish, 6m, GradeType.ActiveParticipation, "Отлично произношение и речник.", today.AddDays(-4));

    var absence1 = await AddDemoAbsenceIfMissingAsync(
        dbContext,
        demoStudent,
        oneBHistory,
        scheduleEntries[0],
        DateOnly.FromDateTime(today.AddDays(-9)),
        1,
        AbsenceType.Absence,
        AbsenceStatus.Excused,
        AbsenceExcuseReason.HealthReasons,
        "Представена медицинска бележка.");
    var absence2 = await AddDemoAbsenceIfMissingAsync(
        dbContext,
        demoStudent,
        oneBMath,
        scheduleEntries[6],
        DateOnly.FromDateTime(today.AddDays(-7)),
        1,
        AbsenceType.Lateness,
        AbsenceStatus.Unexcused,
        null,
        "Закъснение с 10 минути.");

    var feedback1 = await AddDemoFeedbackIfMissingAsync(
        dbContext,
        demoStudent,
        oneBBulgarian,
        scheduleEntries[2],
        DateOnly.FromDateTime(today.AddDays(-1)),
        FeedbackType.Praise,
        "Прочете изразително текста и помогна на съучениците си.");
    var feedback2 = await AddDemoFeedbackIfMissingAsync(
        dbContext,
        demoStudent,
        oneBIt,
        scheduleEntries[1],
        DateOnly.FromDateTime(today.AddDays(-6)),
        FeedbackType.Remark,
        "Да носи редовно тетрадката си за часа.");

    var testEvent = await AddDemoEventIfMissingAsync(
        dbContext,
        principalUser,
        oneBMath,
        new[] { classOneB },
        "Контролно по математика",
        "Кратко контролно върху събиране, изваждане и текстови задачи.",
        EventType.Test,
        today.AddDays(2).Date.AddHours(9));
    var tripEvent = await AddDemoEventIfMissingAsync(
        dbContext,
        principalUser,
        null,
        new[] { classOneB, classTwoA },
        "Посещение на природонаучен музей",
        "Обща училищна екскурзия с образователна програма.",
        EventType.Trip,
        today.AddDays(10).Date.AddHours(8));
    var homeworkEvent = await AddDemoEventIfMissingAsync(
        dbContext,
        primaryTeacherUser,
        oneBBulgarian,
        new[] { classOneB },
        "Домашна работа по литература",
        "Прочетете разказа и подгответе три въпроса за дискусия.",
        EventType.Homework,
        today.AddDays(4).Date.AddHours(15));

    await AddDemoNewsIfMissingAsync(
        dbContext,
        principalUser,
        "Демо: начало на учебната седмица",
        "През тази седмица са планирани контролни работи, родителска среща и посещение на музей.",
        today.AddDays(-1).AddHours(8));
    await AddDemoNewsIfMissingAsync(
        dbContext,
        principalUser,
        "Демо: напомняне за родителска среща",
        "Родителската среща за начален етап ще се проведе в четвъртък от 18:00 ч.",
        today.AddDays(-3).AddHours(10));

    var chat = await UpsertDemoChatAsync(dbContext, "Демо чат: 1Б", principalUser, primaryTeacherUser, studentUser, parentUser);
    var message1 = await AddDemoMessageIfMissingAsync(dbContext, chat, primaryTeacherUser, "Здравейте, утре ще преговорим материала за контролното.", today.AddDays(-1).AddHours(14));
    var message2 = await AddDemoMessageIfMissingAsync(dbContext, chat, studentUser, "Благодаря, ще подготвя задачите от тетрадката.", today.AddDays(-1).AddHours(15));

    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Grade, "Нова оценка по История: Отличен 6.", today.AddDays(-2), grade1, null, null, null, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Grade, "Нова оценка по Математика: Много добър 5.", today.AddDays(-5), grade2, null, null, null, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Absence, "Добавено е извинено отсъствие по История.", today.AddDays(-9), null, absence1, null, null, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Absence, "Добавено е закъснение по Математика.", today.AddDays(-7), null, absence2, null, null, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Feedback, "Нов отзив по Български език и литература.", today.AddDays(-1), null, null, feedback1, null, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Event, "Предстоящо контролно по математика.", today.AddDays(-1), null, null, null, testEvent, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, primaryTeacherUser, NotificationType.Event, "Предстоящо посещение на природонаучен музей.", today.AddDays(-1), null, null, null, tripEvent, null, null);
    await AddDemoNotificationIfMissingAsync(dbContext, studentUser, NotificationType.Message, "Ново съобщение в Демо чат: 1Б.", today.AddDays(-1).AddHours(14), null, null, null, null, chat, message1);
    await AddDemoNotificationIfMissingAsync(dbContext, primaryTeacherUser, NotificationType.Message, "Ново съобщение от Николай Димитров.", today.AddDays(-1).AddHours(15), null, null, null, null, chat, message2);

    _ = grade3;
    _ = grade4;
    _ = feedback2;
    _ = homeworkEvent;

    await dbContext.SaveChangesAsync();

    logger.LogInformation(
        "Demo data seed completed. Principal: {PrincipalEmail}, Teacher: {TeacherEmail}, Student: {StudentEmail}",
        principalEmail,
        primaryTeacherEmail,
        studentEmail);
}

static async Task<User> UpsertDemoUserAsync(
    NemeBookDbContext dbContext,
    IPasswordHasher passwordHasher,
    string email,
    string password,
    string firstName,
    string? middleName,
    string lastName,
    UserRole role)
{
    var normalizedEmail = email.Trim().ToLowerInvariant();
    var user = await dbContext.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(existingUser => existingUser.Email == normalizedEmail);

    if (user is null)
    {
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail
        };
        await dbContext.Users.AddAsync(user);
    }

    user.FirstName = firstName;
    user.MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName;
    user.LastName = lastName;
    user.Password = passwordHasher.HashPassword(password);
    user.Role = role;
    user.IsActive = true;
    user.IsDeleted = false;

    return user;
}

static async Task<Teacher> UpsertDemoTeacherAsync(NemeBookDbContext dbContext, User user, DateOnly birthDate)
{
    var teacher = await dbContext.Teachers
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(existingTeacher => existingTeacher.UserId == user.Id);

    if (teacher is null)
    {
        teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };
        await dbContext.Teachers.AddAsync(teacher);
    }

    teacher.BirthDate = birthDate;
    return teacher;
}

static async Task<Subject> UpsertDemoSubjectAsync(NemeBookDbContext dbContext, string name)
{
    var subject = await dbContext.Subjects.FirstOrDefaultAsync(existingSubject => existingSubject.Name == name);
    if (subject is not null)
    {
        return subject;
    }

    subject = new Subject
    {
        Id = Guid.NewGuid(),
        Name = name
    };
    await dbContext.Subjects.AddAsync(subject);
    return subject;
}

static async Task<TeacherSubject> UpsertDemoTeacherSubjectAsync(NemeBookDbContext dbContext, Teacher teacher, Subject subject)
{
    var teacherSubject = await dbContext.TeacherSubjects
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(existingTeacherSubject =>
            existingTeacherSubject.TeacherId == teacher.Id &&
            existingTeacherSubject.SubjectId == subject.Id);
    if (teacherSubject is not null)
    {
        return teacherSubject;
    }

    teacherSubject = new TeacherSubject
    {
        Id = Guid.NewGuid(),
        TeacherId = teacher.Id,
        SubjectId = subject.Id
    };
    await dbContext.TeacherSubjects.AddAsync(teacherSubject);
    return teacherSubject;
}

static async Task<Class> UpsertDemoClassAsync(NemeBookDbContext dbContext, int gradeNumber, char letter, Teacher mainTeacher)
{
    var schoolClass = await dbContext.Classes
        .FirstOrDefaultAsync(existingClass =>
            existingClass.GradeNumber == gradeNumber &&
            existingClass.Letter == letter);
    if (schoolClass is null)
    {
        schoolClass = new Class
        {
            Id = Guid.NewGuid(),
            GradeNumber = gradeNumber,
            Letter = letter
        };
        await dbContext.Classes.AddAsync(schoolClass);
    }

    schoolClass.MainTeacherId = mainTeacher.Id;
    return schoolClass;
}

static async Task<Student> UpsertDemoStudentAsync(
    NemeBookDbContext dbContext,
    User user,
    Class schoolClass,
    DateOnly birthDate)
{
    var student = await dbContext.Students
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(existingStudent => existingStudent.UserId == user.Id);

    if (student is null)
    {
        student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };
        await dbContext.Students.AddAsync(student);
    }

    student.BirthDate = birthDate;
    student.ClassId = schoolClass.Id;
    return student;
}

static async Task<Parent> UpsertDemoParentAsync(NemeBookDbContext dbContext, User user, Student student)
{
    var parent = await dbContext.Parents
        .IgnoreQueryFilters()
        .Include(existingParent => existingParent.Students)
        .FirstOrDefaultAsync(existingParent => existingParent.UserId == user.Id);

    if (parent is null)
    {
        parent = new Parent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };
        await dbContext.Parents.AddAsync(parent);
    }

    if (parent.Students.All(existingStudent => existingStudent.Id != student.Id))
    {
        parent.Students.Add(student);
    }

    return parent;
}

static async Task<ClassSubject> UpsertDemoClassSubjectAsync(
    NemeBookDbContext dbContext,
    Class schoolClass,
    Subject subject,
    Teacher teacher)
{
    var classSubject = await dbContext.ClassSubjects
        .FirstOrDefaultAsync(existingClassSubject =>
            existingClassSubject.ClassId == schoolClass.Id &&
            existingClassSubject.SubjectId == subject.Id);

    if (classSubject is null)
    {
        classSubject = new ClassSubject
        {
            Id = Guid.NewGuid(),
            ClassId = schoolClass.Id,
            SubjectId = subject.Id
        };
        await dbContext.ClassSubjects.AddAsync(classSubject);
    }

    classSubject.TeacherId = teacher.Id;
    return classSubject;
}

static async Task<ClassScheduleEntry> UpsertDemoScheduleEntryAsync(
    NemeBookDbContext dbContext,
    Class schoolClass,
    ClassSubject classSubject,
    DayOfWeek dayOfWeek,
    int periodNumber,
    string startsAt,
    string endsAt)
{
    var scheduleEntry = await dbContext.ClassScheduleEntries
        .FirstOrDefaultAsync(existingScheduleEntry =>
            existingScheduleEntry.ClassId == schoolClass.Id &&
            existingScheduleEntry.DayOfWeek == dayOfWeek &&
            existingScheduleEntry.PeriodNumber == periodNumber);

    if (scheduleEntry is null)
    {
        scheduleEntry = new ClassScheduleEntry
        {
            Id = Guid.NewGuid(),
            ClassId = schoolClass.Id,
            DayOfWeek = dayOfWeek,
            PeriodNumber = periodNumber
        };
        await dbContext.ClassScheduleEntries.AddAsync(scheduleEntry);
    }

    scheduleEntry.ClassSubjectId = classSubject.Id;
    scheduleEntry.StartsAt = TimeOnly.Parse(startsAt);
    scheduleEntry.EndsAt = TimeOnly.Parse(endsAt);
    return scheduleEntry;
}

static async Task<Grade> AddDemoGradeIfMissingAsync(
    NemeBookDbContext dbContext,
    Student student,
    ClassSubject classSubject,
    decimal value,
    GradeType type,
    string note,
    DateTime createdAt)
{
    var grade = await dbContext.Grades.FirstOrDefaultAsync(existingGrade =>
        existingGrade.StudentId == student.Id &&
        existingGrade.ClassSubjectId == classSubject.Id &&
        existingGrade.Type == type &&
        existingGrade.Note == note);
    if (grade is not null)
    {
        grade.Value = value;
        grade.CreatedAt = createdAt;
        return grade;
    }

    grade = new Grade
    {
        Id = Guid.NewGuid(),
        StudentId = student.Id,
        ClassSubjectId = classSubject.Id,
        Value = value,
        Type = type,
        Note = note,
        CreatedAt = createdAt
    };
    await dbContext.Grades.AddAsync(grade);
    return grade;
}

static async Task<Absence> AddDemoAbsenceIfMissingAsync(
    NemeBookDbContext dbContext,
    Student student,
    ClassSubject classSubject,
    ClassScheduleEntry scheduleEntry,
    DateOnly date,
    int lessonNumber,
    AbsenceType type,
    AbsenceStatus status,
    AbsenceExcuseReason? excuseReason,
    string excuseNote)
{
    var absence = await dbContext.Absences.FirstOrDefaultAsync(existingAbsence =>
        existingAbsence.StudentId == student.Id &&
        existingAbsence.ClassSubjectId == classSubject.Id &&
        existingAbsence.Date == date &&
        existingAbsence.LessonNumber == lessonNumber &&
        existingAbsence.Type == type);
    if (absence is not null)
    {
        absence.Status = status;
        absence.ExcuseReason = excuseReason;
        absence.ExcuseNote = excuseNote;
        absence.ClassScheduleEntryId = scheduleEntry.Id;
        return absence;
    }

    absence = new Absence
    {
        Id = Guid.NewGuid(),
        StudentId = student.Id,
        ClassSubjectId = classSubject.Id,
        ClassScheduleEntryId = scheduleEntry.Id,
        Date = date,
        LessonNumber = lessonNumber,
        Type = type,
        Status = status,
        ExcuseReason = excuseReason,
        ExcuseNote = excuseNote,
        CreatedAt = date.ToDateTime(new TimeOnly(lessonNumber + 7, 0))
    };
    await dbContext.Absences.AddAsync(absence);
    return absence;
}

static async Task<Feedback> AddDemoFeedbackIfMissingAsync(
    NemeBookDbContext dbContext,
    Student student,
    ClassSubject classSubject,
    ClassScheduleEntry scheduleEntry,
    DateOnly date,
    FeedbackType type,
    string description)
{
    var feedback = await dbContext.Feedbacks.FirstOrDefaultAsync(existingFeedback =>
        existingFeedback.StudentId == student.Id &&
        existingFeedback.ClassSubjectId == classSubject.Id &&
        existingFeedback.Date == date &&
        existingFeedback.Type == type &&
        existingFeedback.Description == description);
    if (feedback is not null)
    {
        feedback.ClassScheduleEntryId = scheduleEntry.Id;
        return feedback;
    }

    feedback = new Feedback
    {
        Id = Guid.NewGuid(),
        StudentId = student.Id,
        ClassSubjectId = classSubject.Id,
        ClassScheduleEntryId = scheduleEntry.Id,
        Date = date,
        CreatedAt = date.ToDateTime(new TimeOnly(12, 0)),
        Type = type,
        Description = description
    };
    await dbContext.Feedbacks.AddAsync(feedback);
    return feedback;
}

static async Task<Event> AddDemoEventIfMissingAsync(
    NemeBookDbContext dbContext,
    User createdBy,
    ClassSubject? classSubject,
    IReadOnlyCollection<Class> classes,
    string title,
    string description,
    EventType eventType,
    DateTime date)
{
    var schoolEvent = await dbContext.Events
        .Include(existingEvent => existingEvent.Classes)
        .FirstOrDefaultAsync(existingEvent =>
            existingEvent.Title == title &&
            existingEvent.EventType == eventType);
    if (schoolEvent is null)
    {
        schoolEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            EventType = eventType
        };
        await dbContext.Events.AddAsync(schoolEvent);
    }

    schoolEvent.CreatedByUserId = createdBy.Id;
    schoolEvent.ClassSubjectId = classSubject?.Id;
    schoolEvent.Description = description;
    schoolEvent.Date = date;

    foreach (var schoolClass in classes)
    {
        if (schoolEvent.Classes.All(existingClass => existingClass.Id != schoolClass.Id))
        {
            schoolEvent.Classes.Add(schoolClass);
        }
    }

    return schoolEvent;
}

static async Task<News> AddDemoNewsIfMissingAsync(
    NemeBookDbContext dbContext,
    User createdBy,
    string title,
    string text,
    DateTime createdAt)
{
    var news = await dbContext.News.FirstOrDefaultAsync(existingNews => existingNews.Title == title);
    if (news is null)
    {
        news = new News
        {
            Id = Guid.NewGuid(),
            Title = title
        };
        await dbContext.News.AddAsync(news);
    }

    news.Text = text;
    news.CreatedAt = createdAt;
    news.CreatedByUserId = createdBy.Id;
    return news;
}

static async Task<Chat> UpsertDemoChatAsync(NemeBookDbContext dbContext, string name, params User[] users)
{
    var chat = await dbContext.Chats
        .Include(existingChat => existingChat.Users)
        .FirstOrDefaultAsync(existingChat => existingChat.Name == name);
    if (chat is null)
    {
        chat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = name
        };
        await dbContext.Chats.AddAsync(chat);
    }

    foreach (var user in users)
    {
        if (chat.Users.All(existingUser => existingUser.Id != user.Id))
        {
            chat.Users.Add(user);
        }
    }

    return chat;
}

static async Task<Message> AddDemoMessageIfMissingAsync(
    NemeBookDbContext dbContext,
    Chat chat,
    User sender,
    string text,
    DateTime sentAt)
{
    var message = await dbContext.Messages.FirstOrDefaultAsync(existingMessage =>
        existingMessage.ChatId == chat.Id &&
        existingMessage.SenderId == sender.Id &&
        existingMessage.Text == text);
    if (message is not null)
    {
        message.SentAt = sentAt;
        message.IsDeleted = false;
        return message;
    }

    message = new Message
    {
        Id = Guid.NewGuid(),
        ChatId = chat.Id,
        SenderId = sender.Id,
        Text = text,
        SentAt = sentAt
    };
    await dbContext.Messages.AddAsync(message);
    return message;
}

static async Task<Notification> AddDemoNotificationIfMissingAsync(
    NemeBookDbContext dbContext,
    User user,
    NotificationType type,
    string text,
    DateTime createdAt,
    Grade? grade,
    Absence? absence,
    Feedback? feedback,
    Event? schoolEvent,
    Chat? chat,
    Message? message)
{
    var notification = await dbContext.Notifications.FirstOrDefaultAsync(existingNotification =>
        existingNotification.UserId == user.Id &&
        existingNotification.Type == type &&
        existingNotification.Text == text);
    if (notification is null)
    {
        notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = type,
            Text = text
        };
        await dbContext.Notifications.AddAsync(notification);
    }

    notification.CreatedAt = createdAt;
    notification.GradeId = grade?.Id;
    notification.AbsenceId = absence?.Id;
    notification.FeedbackId = feedback?.Id;
    notification.EventId = schoolEvent?.Id;
    notification.ChatId = chat?.Id;
    notification.MessageId = message?.Id;
    notification.IsRead = false;

    return notification;
}
