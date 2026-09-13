# TokenAuthDemo

[![GitHub repo](https://img.shields.io/badge/GitHub-TokenAuthDemo-181717?logo=github)](https://github.com/nguyenuthien0209/TokenAuthDemo)
[![build](https://github.com/nguyenuthien0209/TokenAuthDemo/actions/workflows/build.yml/badge.svg)](https://github.com/nguyenuthien0209/TokenAuthDemo/actions/workflows/build.yml)

An ASP.NET Core 8 Web API secured by **Duende IdentityServer** — a real OAuth2 / OpenID
Connect authorization server, not a hand-rolled token scheme. Register a user, log in
against Duende's standard `/connect/token` endpoint to get a real signed JWT, and call
`[Authorize]`-protected endpoints (including role-based authorization) with it.

> This project previously issued its own hand-rolled JWTs with zero NuGet dependencies
> (useful in network-restricted environments). It has since been migrated to Duende
> IdentityServer + ASP.NET Core Identity, which is the standards-based way to do this in
> .NET. See "Duende IdentityServer" below for what that means architecturally and
> licensing-wise.

## Duende IdentityServer

[Duende IdentityServer](https://duendesoftware.com/products/identityserver) is a
standalone OAuth2/OIDC authorization server, not a REST API with `/login` and
`/refresh` routes. Clients get tokens by talking to its standard endpoints directly:

| Endpoint | Purpose |
| --- | --- |
| `POST /connect/token` | Get an access token (and refresh token). Replaces the old `POST /api/auth/login` and `/refresh`. |
| `POST /connect/revocation` | Revoke a token. Replaces the old `POST /api/auth/revoke`. |
| `GET /.well-known/openid-configuration` | Discovery document — lets a client find every other endpoint (and the signing key) automatically. |

**This app and the token issuer are the same process** — `Program.cs` runs both
`AddIdentityServer(...)` (the token issuer) and the `/api/secure/*` controllers (the
protected resource) side by side, and validates tokens via Duende's `AddLocalApi()` (an
in-process check, with no HTTP round-trip to itself). In a larger system these would
usually be two separate services — one issuing tokens, one or more APIs validating
them via `AddJwtBearer` + `Authority` pointed at the issuer's URL — but combining them
keeps this demo runnable as a single `dotnet run`.

**⚠️ Licensing**: Duende IdentityServer requires a paid license for production use. It's
free for development/testing (which is what this demo runs under — you'll see a
license warning in the startup log, that's expected) and for companies under a revenue
threshold via Duende's Community Edition; see
[duendesoftware.com/products/identityserver#pricing](https://duendesoftware.com/products/identityserver#pricing)
for current terms before deploying this anywhere real. [OpenIddict](https://documentation.openiddict.com/)
is a free, MIT-licensed alternative implementing the same OAuth2/OIDC standards if a
license isn't an option.

### The `demo-client`

`IdentityServerHost/Config.cs` defines one client, `demo-client`, using the **Resource
Owner Password** grant (`grant_type=password` — the client collects username/password
itself and exchanges them for tokens directly), with no client secret. This mirrors the
old demo's "log in with a username and password" UX and is what makes the curl/Swagger
examples below work with a single request. It's the one part of OAuth2 that's genuinely
becoming legacy (OAuth 2.1 drops it in favor of Authorization Code + PKCE for anything
with a browser) — kept here for parity with the original API shape, not as a
recommendation for a real client.

### Roles / the seeded admin user

In `Development`, `Program.cs` seeds an `Admin` role and one `admin` / `Admin123!` user
on startup (in-memory, so it's re-seeded every restart) — registering through the API
only ever creates ordinary users, so without this there'd be no way to demonstrate
`GET /api/secure/admin-only` actually succeeding.

## Project layout

```
Program.cs                        Composition root: Identity, IdentityServer, auth pipeline, Swagger, dev seed data
IdentityServerHost/Config.cs       IdentityServer's clients / scopes / identity resources (in-memory)
Data/ApplicationDbContext.cs       EF Core Identity store (InMemory provider — swap for a real DB)
Models/ApplicationUser.cs          : IdentityUser — the app's user type
Dtos/AuthDtos.cs                   RegisterRequest / RegisterResponse / ErrorResponse
Controllers/AuthController.cs      POST /api/auth/register only (login/refresh/revoke are IdentityServer's /connect/* endpoints)
Controllers/SecureController.cs    GET /api/secure/me, /api/secure/admin-only (protected)
```

## Running it

```bash
dotnet run
```

By default this listens on the URL(s) in `Properties/launchSettings.json`
(`dotnet run` prints the actual URL on startup) and opens the Swagger UI. The startup
log will show a Duende license warning (expected in dev/test, see above) and an
in-memory persisted-grant-store notice (refresh tokens/consent live in memory only —
fine for a demo, swap for `AddOperationalStore` (EF Core) to persist them for real).

## Swagger / OpenAPI

In the `Development` environment the app serves interactive docs:

| URL | What it is |
| --- | --- |
| `/` | Redirects to `/swagger` |
| `/swagger` | Swagger UI |
| `/swagger/v1/swagger.json` | The generated OpenAPI 3 document |

To call the protected endpoints from the UI:

1. `POST /api/auth/register` to create a user (or skip this and use the seeded `admin` / `Admin123!`).
2. Click **Authorize** at the top right. Under the `oauth2` scheme, enter the username/password,
   leave `client_id` as `demo-client` (no secret needed), pick the scopes you want (`api1` is
   required for the API to accept the token), and click Authorize — Swagger calls
   `POST /connect/token` for you and stores the resulting access token.
3. `GET /api/secure/me` now returns 200 with your claims.

`GenerateDocumentationFile` is on, so `///` summaries on controllers and DTOs show up in the UI.

## Calling it with curl

```bash
BASE=http://localhost:5171   # use whatever URL dotnet run printed

# Register
curl -X POST $BASE/api/auth/register -H 'Content-Type: application/json' \
  -d '{"username":"alice","email":"alice@example.com","password":"SuperSecret123"}'

# Log in — Duende's standard OAuth2 token endpoint (form-urlencoded, not JSON).
# offline_access is what makes it return a refresh_token; api1 is what makes the
# access_token work on /api/secure/*.
curl -X POST $BASE/connect/token -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'grant_type=password&client_id=demo-client&username=alice&password=SuperSecret123&scope=openid profile roles api1 offline_access'

# Call a protected endpoint
curl $BASE/api/secure/me -H "Authorization: Bearer <access_token>"

# Get a new access token when the old one expires (rotates the refresh token too)
curl -X POST $BASE/connect/token -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'grant_type=refresh_token&client_id=demo-client&refresh_token=<refresh_token>'

# Log out / revoke a refresh token
curl -X POST $BASE/connect/revocation -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=demo-client&token=<refresh_token>&token_type_hint=refresh_token'
```

See `TokenAuthDemo.http` for the same requests as a ready-to-run `.http` file
(including the admin-only call and the discovery document).

## Security properties implemented

- **Password storage** via ASP.NET Core Identity's default hasher (PBKDF2-HMAC-SHA256).
- **Short-lived access tokens** (15 min, RS256-signed JWTs) — signature, expiry, issuer
  and audience are all validated by Duende on every request.
- **Rotating refresh tokens** (7 day absolute lifetime, one-time use): each refresh call
  invalidates the token used and issues a new one; reusing an already-rotated refresh
  token is rejected (`invalid_grant`).
- **Explicit revocation** via `POST /connect/revocation`.
- **Role-based authorization**: `GET /api/secure/admin-only` requires the `Admin` role,
  enforced via an explicit claims policy (see the "AdminOnly" comment in `Program.cs`
  for why it's not the simpler `[Authorize(Roles = "Admin")]`).
- Tokens are signed with a locally-generated RSA key (`AddDeveloperSigningCredential()`,
  written to `tempkey.rsa`) — fine for dev, replace with a real certificate
  (`AddSigningCredential(cert)`) before deploying anywhere real.

## Verified behavior (actually run, not just written)

All of the following were exercised against a running instance in this session:

- Register → 200; duplicate username/email → 400 with Identity's validation errors
- `POST /connect/token` (password grant) → 200 with a real RS256 JWT, `expires_in: 900`, and a refresh token
- `GET /api/secure/me` with no token → 401; with a valid token → 200 with claims
- `GET /api/secure/admin-only` as an ordinary user → 403; as the seeded `admin` user → 200, with a `role: Admin` claim visible in `/api/secure/me`
- Refresh rotates the token; reusing the *old* refresh token afterward → `400 invalid_grant`
- `POST /connect/revocation` → 200, and the revoked refresh token then fails to refresh → `400 invalid_grant`
- Swagger UI's generated `swagger.json` correctly lists only the remaining endpoints and the `oauth2` password-flow security scheme pointed at `/connect/token`

## Before deploying anywhere real

- Get a Duende license (see the licensing note above) or migrate to OpenIddict.
- Replace `AddDeveloperSigningCredential()` with a real certificate.
- Replace the EF Core InMemory provider (`Program.cs`) with a real database, and add
  `AddOperationalStore` for IdentityServer's own persisted grants (refresh tokens,
  consent) so they survive a restart and work across multiple instances.
- Tighten the password policy in `Program.cs` (it's relaxed to `MinLength = 8` only,
  matching the old demo's validation, for convenience).
- Put everything behind HTTPS and reconsider the Resource Owner Password grant — prefer
  Authorization Code + PKCE for anything with a browser in front of it.
