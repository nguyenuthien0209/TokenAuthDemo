# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet restore              # restore NuGet packages (nuget.org is configured in NuGet.Config)
dotnet build                # build; should be 0 warnings, 0 errors
dotnet run                  # run — opens Swagger UI at /swagger (see launchSettings.json for the port)
```

There is no automated test suite. Verify changes by actually running the app and
exercising the relevant endpoint(s) via `TokenAuthDemo.http`, curl, or Swagger UI —
see the "Calling it with curl" section of `README.md` for the exact request shapes
(most importantly: logging in is a form-urlencoded POST to `/connect/token`, not JSON
to a custom route). CI (`.github/workflows/build.yml`) runs `dotnet restore` +
`dotnet build` on every push/PR to `main` — no test step to mirror locally.

On first run, Duende IdentityServer writes a local RSA signing key to `tempkey.jwk`
next to the app (via `AddDeveloperSigningCredential()`). It's gitignored and
regenerates automatically — never commit it, and don't be surprised if it appears
after a local run.

## Architecture

This is an ASP.NET Core 8 Web API where **the token issuer and the API it protects run
in the same process**. `Program.cs` is the composition root and wires up three things
side by side:

1. **ASP.NET Core Identity** (`AddIdentity<ApplicationUser, IdentityRole>`) — user
   accounts, roles, password hashing. Backed by `Data/ApplicationDbContext.cs`, an
   `IdentityDbContext` on EF Core's **InMemory** provider (data lost on restart by
   design — swap the provider to persist for real).
2. **Duende IdentityServer** (`AddIdentityServer(...)`) — the actual OAuth2/OIDC
   authorization server. Its clients/scopes/identity-resources are defined in-code in
   `IdentityServerHost/Config.cs` (not a database). `AddAspNetIdentity<ApplicationUser>()`
   bridges it to (1) so login credentials are validated against Identity.
3. **The protected API** (`Controllers/SecureController.cs`) — validates tokens via
   Duende's `AddLocalApi()`, an **in-process** check against the token issuer running
   in this same app (no HTTP round-trip to a discovery endpoint, unlike the more
   common cross-service `AddJwtBearer` + `Authority` setup).

Because issuer and API are combined, there are **no custom `/login`, `/refresh`, or
`/revoke` routes** — those are Duende's standard endpoints instead:
`POST /connect/token` (login via `grant_type=password`, and refresh via
`grant_type=refresh_token`), `POST /connect/revocation`, and
`GET /.well-known/openid-configuration` for discovery. `Controllers/AuthController.cs`
only exposes `POST /api/auth/register` (account creation is still app-specific;
obtaining a token from those credentials afterward is Duende's job).

The one client is `demo-client` (`IdentityServerHost/Config.cs`), using the Resource
Owner Password grant (public, no secret) specifically so a single request can log in
with a username/password — matching the original hand-rolled demo's UX and letting
Swagger UI's OAuth2 "password" flow work out of the box.

`GET /api/secure/admin-only` requires the `Admin` role, but does **not** use
`[Authorize(Roles = "Admin")]` — it uses an explicit `RequireClaim("role", "Admin")`
authorization policy (`"AdminOnly"` in `Program.cs`) instead, because the
locally-validated token's `ClaimsIdentity.RoleClaimType` doesn't reliably match
Duende's `"role"` claim type. Follow this pattern for any other role/claim check
rather than reaching for `[Authorize(Roles = ...)]`.

In `Development` only, `Program.cs` seeds an `Admin` role and one `admin` / `Admin123!`
user on startup (`SeedDevelopmentDataAsync`) — registering through the API alone never
grants admin, so without this there'd be no account to test the admin-only endpoint
with.

### History

This project originally implemented JWT issuance/validation entirely by hand (custom
`AuthenticationHandler`, HMAC-signed tokens, PBKDF2 hashing, zero third-party NuGet
packages) to run in a network-restricted sandbox. It was later migrated to Duende
IdentityServer + ASP.NET Core Identity, the standards-based approach, once real NuGet
access was available. If old code, comments, or docs mention hand-rolled `TokenService`,
`JwtAuthenticationHandler`, or a `Jwt:` config section, that's stale — the current
implementation is entirely IdentityServer-based.

### Licensing

Duende IdentityServer requires a paid license for production use (free for dev/test —
which is what this app runs under, hence the license warning visible in the startup
log — and for companies under a revenue threshold via Duende's Community Edition).
See the "Duende IdentityServer" section of `README.md` for details and the OpenIddict
alternative if a license isn't viable. This repository's own MIT license (`LICENSE`)
covers only the code written here, not Duende IdentityServer itself.
