using Data;
using Data.Repositories;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Interfaces.Chats;
using Services.Interfaces.Registration;
using Services.Interfaces.Security;
using Services.Interfaces.Students;
using Services.Options;
using Services.Repositories;
using Services.Services.Accounts;
using Services.Services.Auth;
using Services.Services.Chats;
using Services.Services.Email;
using Services.Services.Notifications;
using Services.Services.Registration;
using Services.Services.Security;
using Services.Services.Students;

var builder = WebApplication.CreateBuilder(args);
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..",".env"));
builder.Configuration.AddEnvironmentVariables();
// Add services to the container.
builder.Services.AddDbContext<NemeBookDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<RateLimitingOptions>();
// Register repositories.
builder.Services.AddScoped<IAbsenceRepository, AbsenceRepository>();
builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
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

// Register services.
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IEmailService>(serviceProvider => serviceProvider.GetRequiredService<EmailService>());
builder.Services.AddScoped<IRegistrationEmailSender>(serviceProvider => serviceProvider.GetRequiredService<EmailService>());
builder.Services.AddScoped<IInvitationTokenService, InvitationTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IStudentService, StudentService>();

builder.Services.Configure<RegistrationEmailOptions>(
    builder.Configuration.GetSection("RegistrationEmail"));

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<RateLimitingOptions>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();