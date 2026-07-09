// Web/Controllers/AccountController.cs
using System.Security.Claims;
using Entities.Enums;
using Entities.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Dtos.Registration;
using Services.Interfaces;
using Services.Interfaces.Registration;

namespace Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAccountService _accountService;
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        IAccountService accountService,
        IRegistrationService registrationService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _accountService = accountService;
        _registrationService = registrationService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(request);
        }

        var user = await _authService.LoginAsync(request);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Невалиден имейл или парола.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(request);
        }

        await SignInUserAsync(user, request.RememberMe);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new ForgotPasswordRequest());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var resetUrlBase = Url.Action(
            nameof(ResetPassword),
            "Account",
            values: null,
            protocol: Request.Scheme);

        if (string.IsNullOrWhiteSpace(resetUrlBase))
        {
            ModelState.AddModelError(string.Empty, "Неуспешно създаване на линк за възстановяване.");
            return View(request);
        }

        var emailSent = await _accountService.SendPasswordResetAsync(request.Email, resetUrlBase, cancellationToken);
        if (!emailSent)
        {
            ModelState.AddModelError(nameof(request.Email), "Няма профил с този имейл адрес.");
            return View(request);
        }

        return View("ChangePasswordWaiting");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)
            || !await _accountService.IsPasswordResetTokenValidAsync(token, cancellationToken))
        {
            TempData["ErrorMessage"] = "Линкът за възстановяване е невалиден или е изтекъл.";
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _accountService.ResetPasswordAsync(model.Token, model.Password, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password reset failed.");
            ModelState.AddModelError(string.Empty, GetPasswordResetErrorMessage(ex.Message));
            return View(model);
        }

        TempData["SuccessMessage"] = "Паролата е сменена успешно. Вече можете да влезете в профила си.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        if (!await IsInvitationValidAsync(token, RegistrationInvitationType.SetPassword, cancellationToken))
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new SetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _registrationService.CompleteSetPasswordAsync(new CompleteSetPasswordRequest
            {
                Token = model.Token,
                Password = model.Password,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Set password invitation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Паролата е създадена успешно. Вече можете да влезете в профила си.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ParentSignUp(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        if (!await IsInvitationValidAsync(token, RegistrationInvitationType.ParentSignUp, cancellationToken))
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ParentSignUpViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ParentSignUp(ParentSignUpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _registrationService.CompleteParentSignUpAsync(new CompleteParentSignUpRequest
            {
                Token = model.Token,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Parent sign-up invitation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Регистрацията е завършена успешно. Вече можете да влезете в профила си.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var userInfo = new UserInfoDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
        };

        return View(userInfo);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction(nameof(Login), "Account");
        }

        var user = await _authService.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return RedirectToAction(nameof(Login), "Account");
        }

        var resetUrlBase = Url.Action(
            nameof(ResetPassword),
            "Account",
            values: null,
            protocol: Request.Scheme);

        if (string.IsNullOrWhiteSpace(resetUrlBase))
        {
            TempData["ErrorMessage"] = "Неуспешно създаване на линк за смяна на паролата.";
            return RedirectToAction(nameof(Profile), "Account");
        }

        await _accountService.SendPasswordResetAsync(user.Email, resetUrlBase, cancellationToken);

        return View("ChangePasswordWaiting");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _authService.ChangePasswordAsync(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword);

        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Текущата парола е грешна.");
            return View(request);
        }

        TempData["SuccessMessage"] = "Паролата е сменена успешно.";
        return RedirectToAction("Profile", "Account");
    }

    private async Task SignInUserAsync(Entities.Models.User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FullName", FormatFullName(user.FirstName, user.MiddleName, user.LastName)),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddDays(1),
            AllowRefresh = true,
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    private static string FormatFullName(string firstName, string? middleName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, middleName, lastName }
                .Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    private async Task<bool> IsInvitationValidAsync(
        string token,
        RegistrationInvitationType type,
        CancellationToken cancellationToken)
    {
        try
        {
            await _registrationService.ValidateInvitationAsync(token, type, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Registration invitation validation failed.");
            TempData["ErrorMessage"] = GetInvitationErrorMessage(ex.Message);
            return false;
        }
    }

    private static string GetInvitationErrorMessage(string error)
    {
        return error switch
        {
            "Invitation has already been used." => "Тази покана вече е използвана.",
            "Invitation has expired." => "Тази покана е изтекла.",
            "Invitation was not found." => "Поканата не беше намерена.",
            "Invitation type is invalid." => "Поканата е невалидна.",
            _ => "Поканата е невалидна или вече е използвана.",
        };
    }

    private static string GetPasswordResetErrorMessage(string error)
    {
        return error switch
        {
            "Password reset link is invalid." => "Линкът за възстановяване е невалиден.",
            "Password reset link has expired." => "Линкът за възстановяване е изтекъл.",
            "Password must be at least 8 characters." => "Паролата трябва да бъде поне 8 символа.",
            _ => "Неуспешно възстановяване на паролата.",
        };
    }
}
