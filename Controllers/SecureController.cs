using Duende.IdentityServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TokenAuthDemo.Controllers;

/// <summary>Sample endpoints that demonstrate [Authorize] working against tokens issued by Duende IdentityServer.</summary>
[ApiController]
[Route("api/secure")]
[Authorize(IdentityServerConstants.LocalApi.PolicyName)]
public class SecureController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new
        {
            Message = "If you can see this, your access token was valid.",
            Claims = claims,
        });
    }

    [HttpGet("admin-only")]
    [Authorize("AdminOnly")]
    public IActionResult AdminOnly() => Ok(new { Message = "Welcome, admin." });
}
