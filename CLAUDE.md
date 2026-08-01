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
  `BeforeTargets="Compile"`) invokes `dotnet-nswag.dll` against `nswag.json`, which reads
  `openapi.swagger.json` and emits `obj/BiatecRouterApiClient.cs`.
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

## Conventions

- Target framework is `net10.0`; nullable reference types and implicit usings are enabled.
- JSON serialization for the generated client uses Newtonsoft.Json (`jsonLibrary: NewtonsoftJson`
  in `nswag.json`), not `System.Text.Json`.
- The package version is date-stamped automatically:
  `<Version>1.0.0.$([System.DateTime]::Now.ToString(yyyyMMddHH))</Version>` — don't hand-edit it.
- Keep `BiatecRouterConnector/README.md` and the root `README.md` identical; the former is what
  ships inside the NuGet package.
- The `ReceiveMinimum` slippage-protection warning in the README examples is deliberate and should
  not be removed or watered down when adding new examples.
