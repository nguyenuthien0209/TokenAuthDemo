# Contributing to TokenAuthDemo

Thanks for taking a look at this project. It's a demo, so the bar is "keep it
correct and easy to follow" rather than anything heavyweight — but a few things
help contributions land smoothly.

## Getting set up

```bash
git clone https://github.com/nguyenuthien0209/TokenAuthDemo.git
cd TokenAuthDemo
dotnet restore
dotnet run
```

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
`dotnet run` opens Swagger UI at `/swagger` — see `README.md`'s "Swagger / OpenAPI"
section for how to log in there. `TokenAuthDemo.http` has the same requests as
ready-to-run `.http` blocks if you'd rather use curl or an editor's REST client.

On first run, Duende IdentityServer generates a local `tempkey.jwk` signing key next
to the app — it's gitignored and regenerates automatically, so don't worry about it.

## Before you open a PR

- **Build and run it.** `dotnet build` should be clean (0 warnings, 0 errors), and
  you should actually exercise whatever you changed against the running app (curl,
  Swagger, or the `.http` file) rather than just reading the code. There's no
  automated test suite yet — manual verification is the substitute, so include what
  you checked in the PR description (endpoints called, expected vs. actual status
  codes).
- **CI must pass.** Every push and PR to `main` runs `.github/workflows/build.yml`
  (restore + build). Make sure it's green before asking for review.
- **Keep the README in sync.** If you change an endpoint, a config default, or add a
  dependency, update the relevant README section (and `TokenAuthDemo.http`) in the
  same PR — a demo repo's main value is that the docs match the code.
- **Don't commit secrets or generated key material.** `tempkey.jwk` / `tempkey.rsa`
  are gitignored for exactly this reason; if you add a new kind of generated
  credential, gitignore it too rather than relying on remembering not to `git add` it.

## Code style

Match what's already there rather than introducing a new convention:

- Nullable reference types are on (`<Nullable>enable</Nullable>`) — don't silence
  warnings with `!` unless you're genuinely certain, and prefer fixing the underlying
  nullability instead.
- XML doc comments (`///`) on public types/members where the original code has them
  — they show up in Swagger UI via `GenerateDocumentationFile`, so they're
  user-facing, not just internal notes.
- Prefer explaining *why* in comments (a design tradeoff, a gotcha, a link to Duende's
  docs) over restating *what* the code obviously does.

## Reporting issues

Open a GitHub issue with what you expected, what happened instead, and enough to
reproduce it (the request you made and the response you got, or the exact error).

## License

By contributing, you agree your contribution is licensed under this project's
[MIT License](LICENSE).
