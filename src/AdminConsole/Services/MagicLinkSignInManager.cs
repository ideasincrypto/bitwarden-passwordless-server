using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Passwordless.AdminConsole.EventLog.Loggers;
using Passwordless.AdminConsole.Identity;
using Passwordless.AdminConsole.Services.Mail;
using static Passwordless.AdminConsole.EventLog.EventLogEventFunctions;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Passwordless.AdminConsole.Services;

public class MagicLinkSignInManager<TUser> : SignInManager<TUser> where TUser : class
{
    public const string PasswordlessSignInPurpose = "MagicLinkSignIn";

    private readonly IMailService _mailService;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly IEventLogger _eventLogger;

    public MagicLinkSignInManager(UserManager<TUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<TUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<TUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<TUser> confirmation,
        IMailService mailService, IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor,
        IEventLogger eventLogger)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _mailService = mailService;
        _urlHelperFactory = urlHelperFactory;
        _actionContextAccessor = actionContextAccessor;
        _eventLogger = eventLogger;
    }

    public async Task<SignInResult> SendEmailForSignInAsync(string email, string? returnUrl)
    {
        var user = await UserManager.FindByEmailAsync(email);
        if (user == null)
        {
            return SignInResult.Failed;
        }

        var token = await UserManager.GenerateUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider, PasswordlessSignInPurpose);

        var urlBuilder = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        var endpoint = urlBuilder.PageLink("/Account/Magic", values: new { returnUrl }) ?? urlBuilder.Content("~/");
        await _mailService.SendPasswordlessSignInAsync(endpoint, token, email);

        if (user is ConsoleAdmin admin) _eventLogger.LogEvent(CreateLoginViaMagicLinkEvent(admin));

        return SignInResult.Success;
    }

    public async Task<string> GenerateToken(TUser user)
    {
        var token = await UserManager.GenerateUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider,
            PasswordlessSignInPurpose);

        // remove url unsafe chars
        //token = token.Replace("+", "").Replace("/", "");

        return token;
    }

    public async Task<SignInResult> PasswordlessSignInAsync(TUser user, string token, bool isPersistent)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var attempt = await CheckPasswordlessSignInAsync(user, token);
        return attempt.Succeeded ?
            await SignInOrTwoFactorAsync(user, isPersistent, bypassTwoFactor: true) : attempt;
    }

    public async Task<SignInResult> PasswordlessSignInAsync(string email, string token, bool isPersistent)
    {
        var user = await UserManager.FindByEmailAsync(email);
        if (user == null)
        {
            return SignInResult.Failed;
        }

        return await PasswordlessSignInAsync(user, token, isPersistent);
    }

    public virtual async Task<SignInResult> CheckPasswordlessSignInAsync(TUser user, string token)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var error = await PreSignInCheck(user);
        if (error != null)
        {
            return error;
        }

        // convert back from url safe
        //token = token.Replace("-", "+").Replace("_", "/");
        if (await UserManager.VerifyUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider,
            PasswordlessSignInPurpose, token))
        {
            return SignInResult.Success;
        }

        Logger.LogWarning(2, "User {userId} failed to provide the correct token.",
            await UserManager.GetUserIdAsync(user));
        return SignInResult.Failed;
    }
}