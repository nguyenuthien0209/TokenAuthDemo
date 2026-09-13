using System.Reflection;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TokenAuthDemo.Data;
using TokenAuthDemo.IdentityServerHost;
using TokenAuthDemo.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- User store: ASP.NET Core Identity, EF Core InMemory provider ----
// (data is lost on restart — see ApplicationDbContext.cs for swapping in a real database)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TokenAuthDemo"));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Mirrors the old hand-rolled RegisterRequest validation (password
        // MinLength 8), relaxed from Identity's very strict complexity
        // defaults for demo purposes. Tighten these for production.
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ---- Duende IdentityServer: the actual OAuth2/OIDC token-issuing server ----
builder.Services
    .AddIdentityServer(options =>
    {
        // Adds an "aud" claim (equal to the API scope name) to access tokens.
        options.EmitStaticAudienceClaim = true;
        // We sign with an explicit developer credential below instead — automatic
        // key rotation is a licensed (Business/Enterprise) feature we don't use.
        options.KeyManagement.Enabled = false;
    })
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryClients(Config.Clients)
    .AddAspNetIdentity<ApplicationUser>()
    // Signs tokens with an auto-generated RSA key persisted to tempkey.rsa
    // next to the app — fine for local dev, NOT for production. Swap for
    // .AddSigningCredential(cert) with a real certificate before deploying.
    .AddDeveloperSigningCredential();

// Validates access tokens issued by the IdentityServer instance running in
// *this same process*, without an HTTP round-trip to a discovery endpoint
// (that's what AddJwtBearer + Authority would do instead — appropriate when
// the API is a separate service from the token issuer). See
// https://docs.duendesoftware.com/identityserver/apis/aspnetcore/#introspection-vs-local-validation
builder.Services
    .AddAuthentication()
    .AddLocalApi(options =>
    {
        options.ExpectedScope = Config.ApiScopeName;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(IdentityServerConstants.LocalApi.PolicyName, policy =>
    {
        policy.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    // [Authorize(Roles = "Admin")] relies on the ClaimsIdentity's configured
    // RoleClaimType, which the locally-validated token's principal may not
    // set to match Duende's "role" claim — this policy checks the claim
    // directly instead, so it works regardless of that default.
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        policy.RequireClaim("role", "Admin");
    });
});

builder.Services.AddControllers();

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TokenAuthDemo API",
        Version = "v1",
        Description =
            "API secured by Duende IdentityServer. Call POST /api/auth/register to create " +
            "a user, then press Authorize and sign in (client_id: demo-client, no secret) to " +
            "get a token and call the /api/secure endpoints.",
    });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            // Resource Owner Password flow: Swagger's Authorize dialog collects
            // username/password directly and posts them to /connect/token itself.
            Password = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/connect/token", UriKind.Relative),
                Scopes = new Dictionary<string, string>
                {
                    [Config.ApiScopeName] = "Full access to the TokenAuthDemo API",
                    ["openid"] = "Your user identifier",
                    ["profile"] = "Your profile information",
                    ["roles"] = "Your role(s)",
                    ["offline_access"] = "Get a refresh token",
                },
            },
        },
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
            },
            new[] { Config.ApiScopeName }
        },
    });

    // Surface the /// <summary> comments on controllers and DTOs in the UI.
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Demo-only seed data: an "Admin" role plus one admin/Admin123! user, so
    // GET /api/secure/admin-only has someone to actually demonstrate against
    // (register/login alone only ever create ordinary, non-admin users).
    await SeedDevelopmentDataAsync(app.Services);

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TokenAuthDemo API v1");
        options.DocumentTitle = "TokenAuthDemo API";
        options.OAuthClientId("demo-client");
        options.OAuthScopes(Config.ApiScopeName, "openid", "profile", "roles", "offline_access");
    });

    // Land on the UI instead of a 404 when opening the site root.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// IdentityServer's middleware also runs authentication for the whole
// pipeline (including our own [Authorize]-protected controllers), so a
// separate app.UseAuthentication() call isn't needed.
app.UseIdentityServer();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedDevelopmentDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    if (await userManager.FindByNameAsync("admin") is null)
    {
        var admin = new ApplicationUser { UserName = "admin", Email = "admin@example.com", EmailConfirmed = true };
        await userManager.CreateAsync(admin, "Admin123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
