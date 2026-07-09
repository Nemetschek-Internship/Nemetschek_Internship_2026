// Web/Controllers/AccountController.cs
using System.Security.Claims;
using System.Security.Cryptography;
using Entities.Models;
using Entities.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Dtos.Registration;
using Services.Interfaces;
using Services.Interfaces.Registration;
using Services.Repositories;

namespace Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAccountService _accountService;
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<AccountController> _logger;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetRepository _passwordResetRepository;

    public AccountController(
        IAuthService authService,
        IAccountService accountService,
        IRegistrationService registrationService,
        ILogger<AccountController> logger,
        IEmailService emailService,
        IPasswordResetRepository passwordResetRepository)
    {
        _authService = authService;
        _accountService = accountService;
        _registrationService = registrationService;
        _logger = logger;
        _emailService = emailService;
        _passwordResetRepository = passwordResetRepository;
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
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordRequest());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var resetUrl = Url.Action(
            action: nameof(ResetPassword),
            controller: "Account",
            values: null,
            protocol: Request.Scheme);

        if (string.IsNullOrWhiteSpace(resetUrl))
        {
            _logger.LogError("Failed to generate password reset URL.");
            ModelState.AddModelError(string.Empty, "Could not start password reset. Please try again.");
            return View(request);
        }

        await _accountService.SendPasswordResetAsync(request.Email, resetUrl, cancellationToken);

        return View("ChangePasswordWaiting");
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
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
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
                Password = model.Password
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Set password invitation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Password set successfully. You can now log in.";
        return View("PasswordChangedSuccess");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ParentSignUp(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
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
                Password = model.Password
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Parent sign-up invitation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Registration completed successfully. You can now log in.";
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
            Role = user.Role.ToString()
        };

        return View(userInfo);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _authService.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var token = GenerateSecureToken();

        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        await _passwordResetRepository.CreateOrReplaceAsync(passwordResetToken, cancellationToken);

        var changePasswordLink = Url.Action(
            action: nameof(ChangePasswordConfirm),
            controller: "Account",
            values: new { token },
            protocol: Request.Scheme);

        if (string.IsNullOrWhiteSpace(changePasswordLink))
        {
            _logger.LogError("Failed to generate password change link for user: {UserId}", user.Id);
            TempData["ErrorMessage"] = "Could not generate password change link. Please try again.";
            return RedirectToAction("Profile", "Account");
        }

        var recipientName = FormatFullName(user.FirstName, user.MiddleName, user.LastName);

        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            recipientName,
            changePasswordLink,
            cancellationToken);

        _logger.LogInformation("Password change link sent to user: {Email}", user.Email);

        return View("ChangePasswordWaiting");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePasswordConfirm(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Invalid password change link.";
            return RedirectToAction("Profile", "Account");
        }

        var passwordResetToken = await _passwordResetRepository.GetByTokenAsync(token, cancellationToken);
        if (passwordResetToken is null)
        {
            TempData["ErrorMessage"] = "Invalid password change link.";
            return RedirectToAction("Profile", "Account");
        }

        if (passwordResetToken.ExpiresAt < DateTime.UtcNow)
        {
            await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);

            TempData["ErrorMessage"] = "Password change link has expired. Please request a new one.";
            return RedirectToAction("Profile", "Account");
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value != passwordResetToken.UserId)
        {
            TempData["ErrorMessage"] = "This password change link does not belong to the current user.";
            return RedirectToAction("Profile", "Account");
        }

        ViewData["Token"] = token;
        return View("ChangePassword", new ChangePasswordRequest());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordConfirm(
        ChangePasswordRequest request,
        string? token,
        CancellationToken cancellationToken = default)
    {
        ViewData["Token"] = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            ModelState.AddModelError(string.Empty, "Invalid password change link.");
            return View("ChangePassword", request);
        }

        var passwordResetToken = await _passwordResetRepository.GetByTokenAsync(token, cancellationToken);
        if (passwordResetToken is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid password change link.");
            return View("ChangePassword", request);
        }

        if (passwordResetToken.ExpiresAt < DateTime.UtcNow)
        {
            await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);

            ModelState.AddModelError(string.Empty, "Password change link has expired. Please request a new one.");
            return View("ChangePassword", request);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value != passwordResetToken.UserId)
        {
            ModelState.AddModelError(string.Empty, "This password change link does not belong to the current user.");
            return View("ChangePassword", request);
        }

        if (!ModelState.IsValid)
        {
            return View("ChangePassword", request);
        }

        var result = await _authService.ChangePasswordAsync(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Current password is incorrect.");
            return View("ChangePassword", request);
        }

        await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);

        TempData["SuccessMessage"] = "Password changed successfully.";
        return View("PasswordChangedSuccess");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string? token, CancellationToken cancellationToken = default)
    {
        var passwordResetToken = await ValidatePasswordResetTokenAsync(token, cancellationToken);
        if (passwordResetToken is null)
        {
            TempData["ErrorMessage"] = "Invalid or expired password reset link.";
            return RedirectToAction(nameof(Login));
        }

        ViewData["FormAction"] = nameof(ResetPassword);
        return View("SetPassword", new SetPasswordViewModel { Token = passwordResetToken.Token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(SetPasswordViewModel model, CancellationToken cancellationToken = default)
    {
        var passwordResetToken = await ValidatePasswordResetTokenAsync(model.Token, cancellationToken);
        if (passwordResetToken is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired password reset link.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["FormAction"] = nameof(ResetPassword);
            return View("SetPassword", model);
        }

        var result = await _authService.ResetPasswordAsync(passwordResetToken!.UserId, model.Password, cancellationToken);
        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Could not reset password. Please request a new link.");
            ViewData["FormAction"] = nameof(ResetPassword);
            return View("SetPassword", model);
        }

        await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);

        TempData["SuccessMessage"] = "Password reset successfully.";
        return View("PasswordChangedSuccess");
    }

    private async Task SignInUserAsync(Entities.Models.User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FullName", FormatFullName(user.FirstName, user.MiddleName, user.LastName))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddDays(1),
            AllowRefresh = true
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

    private async Task<PasswordResetToken?> ValidatePasswordResetTokenAsync(
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var passwordResetToken = await _passwordResetRepository.GetByTokenAsync(token, cancellationToken);
        if (passwordResetToken is null)
        {
            return null;
        }

        if (passwordResetToken.ExpiresAt >= DateTime.UtcNow)
        {
            return passwordResetToken;
        }

        await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);
        return null;
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }
}
