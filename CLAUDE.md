# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

`BiatecRouterConnector` is a small .NET 10 NuGet library that wraps the Biatec Router HTTP API
(`https://router.api.biatec.io`) — a DEX swap router for Algorand/AVM chains. It handles ARC-0014
(`SigTx`) authorization and exposes a generated REST client for quoting, routing, and building
unsigned swap transactions.

## Solution layout

- `BiatecRouterConnector.slnx` — solution file (only 2 projects).
- `BiatecRouterConnector/` — the library that gets packed and published to NuGet.
  - `BiatecRouterClient.cs` — thin hand-written wrapper (`BiatecRouterClient`) around the
    generated `BiatecRouterApiClient`. Sets the `Authorization: SigTx <base64>` header and base URL.
  - `BiatecRouterClientOptions.cs` — options record (`BaseUri`, `Authorization`).
  - `AuthorizationHeaderHandler.cs` — optional `DelegatingHandler` for dynamic auth headers.
  - `Extensions.cs` — `ToRouterParams()` maps `Algorand.Algod.Model.TransactionParametersResponse`
    (Algorand SDK type) to the generated `TransactionParametersResponse` (router API type). Field
    types are `ulong` on the router side — cast with `Convert.ToUInt64`, not `ToInt64`.
  - `openapi.swagger.json` — OpenAPI/Swagger doc for the router API, checked into source control.
  - `nswag.json` — NSwag config that generates the client at build time.
  - `README.md` — **this is the file packed into the NuGet package** (`PackageReadmeFile`). Keep
    it in sync with the root `README.md` (they are intentionally duplicated).
- `BiatectRouterConnectorTests/` — NUnit test project (note: "Biatect" typo in the folder name is
  intentional/existing, not a mistake to "fix").
  - `BiatecRouterClientTests.cs` — an integration-style test that talks to real Algorand MainNet
    ALGOd and the live router API. It tolerates network failures (`Assert.Ignore` on 401/403 or
    `HttpRequestException`) so it can run in disconnected CI, but a real network path exercises
    the full flow when available.

## The generated client

`BiatecRouterApiClient` (namespace `BiatecRouterConnector.Generated`) is **not checked in**. It is
generated fresh on every build:

- MSBuild target `GenerateBiatecRouterApiClient` (in `BiatecRouterConnector.csproj`, runs
  `BeforeTargets="CoreCompile"`) invokes `dotnet-nswag.dll` against `nswag.json`, which reads
  `openapi.swagger.json` and emits `obj/BiatecRouterApiClient.cs`.
- **Don't move the `<Compile Include="obj\**\BiatecRouterApiClient.cs">` item back out to a
  top-level `<ItemGroup>`.** It must stay *inside* the target, right after the `<Exec>` call. A
  top-level item group with that wildcard is evaluated once, before any target runs; on a genuinely
  clean checkout (no `obj/BiatecRouterApiClient.cs` yet — every CI run) it would match nothing, so
  the build fails on its first pass even though the `Exec` generates the file a moment later. Putting
  the `<ItemGroup>` inside the target adds it to `@(Compile)` dynamically during that same build.
- `GeneratePackageOnBuild=true` removes `Build` from `Pack`'s dependency chain (the assumption is
  Pack runs *as part of* Build, not the reverse). Running `dotnet pack` by itself on a clean
  checkout therefore fails with `NU5026` ("dll to be packed was not found on disk") — always
  `dotnet build` first, then `dotnet pack --no-build` (see the `publish` job in `ci.yml`).
- The `NSwag.MSBuild` package version and the `runtime` field in `nswag.json` must reference the
  **same** target framework folder (e.g. both `Net100`, or both `Net90`) — check
  `~/.nuget/packages/nswag.msbuild/<version>/tools/` for the available folders when bumping the
  NSwag.MSBuild package version. A mismatch fails with
  `InvalidOperationException: The specified runtime in the document (...) differs from the current process runtime (...)`.
  Update both `nswag.json`'s `"runtime"` and the `Exec Command` path in
  `BiatecRouterConnector.csproj` together.
- Because the client is regenerated from `openapi.swagger.json`, changing that file can change
  generated types (e.g. numeric fields moving between `long`/`ulong`/`int` depending on the
  OpenAPI `format`). After updating the spec, always run a full build — don't assume hand-written
  code (`Extensions.cs`, `BiatecRouterClient.cs`) still compiles against the new generated types.

## Updating the OpenAPI spec

The source of truth is `https://router.api.biatec.io/swagger/v1/swagger.json`. To refresh:

```bash
curl -s https://router.api.biatec.io/swagger/v1/swagger.json -o BiatecRouterConnector/openapi.swagger.json
python3 -m json.tool BiatecRouterConnector/openapi.swagger.json > /tmp/pretty.json && mv /tmp/pretty.json BiatecRouterConnector/openapi.swagger.json
```

Then `dotnet build` to regenerate the client and fix any compile errors that surface (see above).

## Build & test

```bash
dotnet build BiatecRouterConnector.slnx
dotnet test BiatecRouterConnector.slnx
```

There is no separate "generate" step to remember — building always regenerates the client.
`dotnet list <csproj> package --outdated` / `--vulnerable --include-transitive` are the commands
used to check for stale/vulnerable NuGet dependencies across both projects.

Both projects use NuGet lock files (`packages.lock.json`, via
`RestorePackagesWithLockFile` in `Directory.Build.props`). After bumping any `PackageReference`
version, run `dotnet restore --force-evaluate` to refresh the lock files and commit them — CI
restores with `--locked-mode`, which fails the build if a lock file is stale.

## Linting / code style

- `.editorconfig` (repo root) defines formatting and a handful of style rules.
- `Directory.Build.props` enables .NET analyzers (`EnableNETAnalyzers`, `AnalysisLevel=latest`,
  `AnalysisMode=Recommended`) and `EnforceCodeStyleInBuild`, so style violations show up as build
  warnings locally. When `ContinuousIntegrationBuild=true` (set by CI), the same violations become
  build **errors** via `TreatWarningsAsErrors` — this is what CI's build step passes.
- To check formatting the same way CI does:
  ```bash
  dotnet build BiatecRouterConnector.slnx -p:ContinuousIntegrationBuild=true
  dotnet format BiatecRouterConnector.slnx --verify-no-changes
  ```
  Run `dotnet format BiatecRouterConnector.slnx` (no `--verify-no-changes`) to auto-fix.
- **Ordering matters**: `dotnet format` must run *after* a build, not before — the generated
  `BiatecRouterApiClient.cs` doesn't exist until the NSwag `BeforeTargets="CoreCompile"` target has run
  once, and `dotnet format`'s design-time build won't trigger it on a clean checkout.
- The library project (`BiatecRouterConnector.csproj`) has `GenerateDocumentationFile=true` and
  therefore requires XML doc comments (`CS1591`) on all public members — this is enforced as an
  error under `ContinuousIntegrationBuild=true`. The test project suppresses `CS1591` since it
  isn't published.
- `NoWarn` for `CA1708` is set in the library csproj because the NSwag-generated
  `Generated.AMMType`/`AmmType` pair (names that differ only by case) come from the router's
  OpenAPI schema and aren't ours to rename.

## CI/CD (GitHub Actions)

`.github/workflows/ci.yml` runs on every push (any branch) and every pull request:

1. **`build-test`** job: restore (`--locked-mode`) → build (`Release`,
   `ContinuousIntegrationBuild=true` so warnings are errors) → `dotnet format --verify-no-changes`
   → `dotnet test`.
2. **`publish`** job: only runs on `push` to `refs/heads/master`, and only after `build-test`
   succeeds. Restores, builds, and packs `BiatecRouterConnector.csproj` all with
   `-p:VersionIncrement=${{ github.run_number }}` (the GitHub Actions run number always increases,
   so every push to master produces a strictly higher, unique version — no manual version bumping;
   see `VersionIncrement` in `BiatecRouterConnector.csproj` for how it drives both `Version` and
   `AssemblyVersion`, and why it must be passed to *every* step, restore included), then publishes
   to nuget.org using **Trusted Publishing** (OIDC, no stored API key): `NuGet/login@v1` exchanges
   a short-lived GitHub OIDC token (`permissions: id-token: write`) for a 1-hour NuGet API key,
   which `dotnet nuget push --skip-duplicate` then uses.

### One-time setup required (not doable from the repo alone)

Trusted Publishing must be configured on nuget.org before the `publish` job can push successfully:

1. On nuget.org: profile → **Trusted Publishing** → add a policy for
   `scholtz` / `BiatecRouterConnector` / workflow file `ci.yml` (file name only, no path).
2. In the GitHub repo settings, add a repository secret `NUGET_USER` set to the nuget.org
   username (profile name, **not** an API key and not the email address) that owns that policy.
3. The policy stays in a 7-day "pending" state until the first successful publish supplies GitHub's
   repo/owner IDs; after that it's permanent. If 7 days pass with no publish, just re-trigger it on
   nuget.org.

If `NUGET_USER` or the nuget.org policy is missing/misconfigured, the `publish` job fails at the
`NuGet login` step — this is expected until the above is done, and does not indicate a bug in the
workflow.

## Conventions

- Target framework is `net10.0`; nullable reference types and implicit usings are enabled.
- JSON serialization for the generated client uses Newtonsoft.Json (`jsonLibrary: NewtonsoftJson`
  in `nswag.json`), not `System.Text.Json`.
- Version format is `1.0.<VersionIncrement>.<date>` for both the NuGet package `Version`
  (`<date>` = `yyyyMMddHH`) and `AssemblyVersion` (`<date>` = `MMdd`, since the CLR requires every
  `AssemblyVersion` component to be ≤ 65535 — the full `yyyyMMddHH` doesn't fit).
  `VersionIncrement` defaults to `0` for local/dev builds; CI passes
  `-p:VersionIncrement=<run_number>` to every `dotnet` invocation for a given build (restore, build,
  *and* pack — see the comment on `VersionIncrement` in `BiatecRouterConnector.csproj`). Don't try
  to "fix" versioning by hand-editing the csproj's `Version`/`AssemblyVersion` properties directly.
- Keep `BiatecRouterConnector/README.md` and the root `README.md` identical; the former is what
  ships inside the NuGet package.
- The `ReceiveMinimum` slippage-protection warning in the README examples is deliberate and should
  not be removed or watered down when adding new examples.
