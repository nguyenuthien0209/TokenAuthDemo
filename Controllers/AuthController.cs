using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TokenAuthDemo.Dtos;
using TokenAuthDemo.Models;

namespace TokenAuthDemo.Controllers;

/// <summary>
/// Account creation only. Logging in, refreshing, and revoking tokens are now
/// handled by Duende IdentityServer's standard OAuth2 endpoints
/// (POST /connect/token, POST /connect/revocation) instead of custom routes here
/// — see README.md's "Duende IdentityServer" section for example requests.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Username, Email = request.Email };
        IdentityResult result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            string message = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new ErrorResponse(message));
        }

        return Ok(new RegisterResponse(user.Id, user.UserName!, user.Email!));
    }
}
