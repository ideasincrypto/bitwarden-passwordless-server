using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Passwordless.AdminConsole.Services;
using Passwordless.Net;

namespace AdminConsole.Pages;

public class NewAccountModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IScopedPasswordlessClient _passwordlessClient;

    public NewAccountModel(ILogger<IndexModel> logger, IScopedPasswordlessClient passwordlessClient)
    {
        _logger = logger;
        this._passwordlessClient = passwordlessClient;
    }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostToken(string name, string email)
    {
        // Create new account
        var userId = Guid.NewGuid().ToString();
        try
        {
            var token = await _passwordlessClient.CreateRegisterTokenAsync(new RegisterOptions(userId, $"Playground: {email}")
            {
                DisplayName = name,
                Aliases = new HashSet<string>(1) { email },
                AliasHashing = false
            });

            return new JsonResult(token);
        }
        catch (PasswordlessApiException e)
        {
            return StatusCode(e.Details.Status, e.Details);
        }
    }

    public async Task<IActionResult> OnPost(string token)
    {
        var res = await _passwordlessClient.VerifyTokenAsync(token);
        return new JsonResult(res);
    }

    [MaxLength(64)]
    public string Nickname { get; set; }
}