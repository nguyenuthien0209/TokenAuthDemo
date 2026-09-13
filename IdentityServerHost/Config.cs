using Duende.IdentityServer.Models;

namespace TokenAuthDemo.IdentityServerHost;

/// <summary>
/// In-memory OpenID Connect / OAuth2 configuration for Duende IdentityServer.
/// "In-memory" means this config lives in this file rather than a database —
/// swap for <c>AddConfigurationStore</c> (EF Core) if clients/scopes need to
/// be managed at runtime instead of via deployment. See
/// https://docs.duendesoftware.com/identityserver/fundamentals/resources/ for
/// the resources/scopes model this is built on.
/// </summary>
public static class Config
{
    /// <summary>The one API scope this demo protects — grant it to a client to let it call /api/secure/*.</summary>
    public const string ApiScopeName = "api1";

    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            // Exposes the ASP.NET Core Identity role claim in id_tokens / the
            // userinfo endpoint for clients that request the "roles" scope.
            new IdentityResource("roles", "Your role(s)", new[] { "role" }),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new[]
        {
            new ApiScope(ApiScopeName, "Full access to the TokenAuthDemo API")
            {
                // Without this, "role" would only reach the id_token/userinfo
                // (via the "roles" IdentityResource above) — it needs to be
                // listed here too so it ends up as a claim on the *access*
                // token, which is what SecureController's [Authorize(Roles=...)]
                // checks against.
                UserClaims = { "role" },
            },
        };

    public static IEnumerable<Client> Clients =>
        new[]
        {
            new Client
            {
                ClientId = "demo-client",
                ClientName = "TokenAuthDemo sample client",

                // Resource Owner Password Credentials: the client collects the
                // user's username/password itself and exchanges them directly
                // for tokens at POST /connect/token — the closest OAuth2-standard
                // equivalent to the old hand-rolled POST /api/auth/login. OAuth
                // 2.1 deprecates this grant in favor of Authorization Code + PKCE
                // for anything with a browser; it's used here to preserve the
                // original demo's "log in with a username and password" UX and
                // to keep the Swagger "Authorize" button working the same way.
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                // Public demo client: no secret required. Give every real
                // client a secret instead — ClientSecrets = { new Secret("...".Sha256()) }
                // — and leave RequireClientSecret at its default of true.
                RequireClientSecret = false,

                AllowedScopes = { "openid", "profile", "roles", ApiScopeName, "offline_access" },

                // "offline_access" is what makes /connect/token return a refresh_token.
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,      // rotate on every use, like the old refresh-token store did
                RefreshTokenExpiration = TokenExpiration.Absolute,
                AbsoluteRefreshTokenLifetime = 7 * 24 * 60 * 60, // 7 days — matches the old Jwt:RefreshTokenDays default

                AccessTokenLifetime = 15 * 60,                   // 15 minutes — matches the old Jwt:AccessTokenMinutes default
            },
        };
}
