using Microsoft.AspNetCore.Identity;

namespace TokenAuthDemo.Models;

/// <summary>
/// The application's user type, built on ASP.NET Core Identity's <see cref="IdentityUser"/>
/// (UserName, Email, PasswordHash, lockout counters, etc. are all inherited — nothing
/// hand-rolled here anymore). Duende IdentityServer's <c>AddAspNetIdentity&lt;ApplicationUser&gt;()</c>
/// (see Program.cs) bridges this store into token issuance: the Resource Owner Password
/// grant at POST /connect/token validates credentials via <see cref="Microsoft.AspNetCore.Identity.SignInManager{TUser}"/>,
/// and token claims (sub, role, etc.) are populated from <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}"/>.
/// </summary>
public class ApplicationUser : IdentityUser
{
}
