using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TokenAuthDemo.Models;

namespace TokenAuthDemo.Data;

/// <summary>
/// ASP.NET Core Identity's user/role store (users, roles, claims, logins — the
/// standard <c>IdentityDbContext</c> schema). Registered against EF Core's
/// InMemory provider in Program.cs, so the demo still runs with zero external
/// database setup, the same way the old hand-rolled in-memory <c>IUserStore</c>
/// did. Data is lost on restart. Swap the provider in Program.cs
/// (<c>UseSqlServer</c> / <c>UseSqlite</c> / <c>UseNpgsql</c>) to persist for real —
/// nothing else in the project needs to change.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
