# BiatecRouterConnector

[![CI/CD](https://github.com/scholtz/BiatecRouterConnector/actions/workflows/ci.yml/badge.svg)](https://github.com/scholtz/BiatecRouterConnector/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BiatecRouterConnector.svg)](https://www.nuget.org/packages/BiatecRouterConnector)

NuGet library for calling the Biatec Router API (routing swaps and generating transactions to be signed on Algorand / AVM chains).

## Authorization

The API uses an `Authorization` header carrying an ARC-0014 authentication transaction. The snippet below mirrors the working integration test.

```csharp
using Algorand;
using Algorand.Algod;
using BiatecRouterConnector;

// 1) Use ALGOd (MainNet) for ARC14 Authorization
using var algodHttpClient = HttpClientConfigurator.ConfigureHttpClient(AlgodConfiguration.MainNet);
var algodApiInstance = new DefaultApi(algodHttpClient);
var txparams = await algodApiInstance.TransactionParamsAsync();
txparams.Fee = 0;

// 2) Create and sign an ARC-0014 auth transaction.
var account = AlgorandARC76AccountDotNet.ARC76.GetAccount("<MNEMONIC>");
var tx = Algorand.Algod.Model.Transactions.PaymentTransaction
    .GetPaymentTransactionFromNetworkTransactionParameters(
        account.Address,
        account.Address,
        0,
        "BiatecRouter#ARC14",
        txparams);

var signed = tx.Sign(account);
var authHeader = Convert.ToBase64String(Algorand.Utils.Encoder.EncodeToMsgPackOrdered(signed));

// 3) Construct the client. The Authorization header is attached automatically.
var routerHttpClient = new HttpClient();
var client = new BiatecRouterClient(routerHttpClient, authorization: authHeader);
```

You can also configure the client with `BiatecRouterClientOptions` (e.g. to point at a different environment):

```csharp
var client = new BiatecRouterClient(routerHttpClient, new BiatecRouterClientOptions
{
    BaseUri = new Uri("https://router.api.biatec.io"),
    Authorization = authHeader
});
```

The endpoints below (`quote`, `route`, `stats`, `snapshot`) require only the `Authorization` header; `routeTxs` additionally needs valid network transaction parameters (see the example there).

## Get a quoted output amount

Cheapest way to preview a swap: returns just the expected output amount for the given input.

```csharp
// Quote swapping 1 ALGO (0) -> USDC (31566704).
ulong outputAmount = await client.Api.QuoteAsync(fromAsset: 0, toAsset: 31566704, amount: 1_000_000);

Console.WriteLine($"Expected output: {outputAmount} base units of USDC");
```

## Get a detailed route

Returns hop allocations and pool splits without generating transactions.

```csharp
var routes = await client.Api.RouteAsync(fromAsset: 0, toAsset: 31566704, amount: 1_000_000, maxRoutes: 3);

foreach (var route in routes)
{
    Console.WriteLine($"{route.InputAmount} -> {route.OutputAmount} across {route.Hops.Count} hop(s), " +
                       $"network fee: {route.TotalNetworkFeeMicroAlgos} microAlgos");
}
```

## Request unsigned transactions for a swap (routeTxs)

> IMPORTANT: `ReceiveMinimum` is your slippage protection.
> Setting it too low (e.g. `1`, as in the example) effectively disables protection and can result in receiving far less than expected.
> In production, compute `ReceiveMinimum` from your quoted output amount and an explicit slippage tolerance (and consider fees and decimal precision).

```csharp
var result = await client.Api.RouteTxsAsync(new BiatecRouterConnector.Generated.RouteInputParameters
{
    FromAsset = 0,
    ToAsset = 31566704,
    SwapAmount = 1_000_000, // 1 ALGO
    ReceiveMinimum = 1, // WARNING: example only; do NOT use this value in production.
    Sender = account.Address.ToString(),
    TransParams = txparams.ToRouterParams()
});

// result.Routes[0].TxsToSign contains base64-encoded unsigned transactions to be signed and submitted.
var firstRoute = result.Routes.First();
foreach (var unsignedTxB64 in firstRoute.TxsToSign)
{
    var unsignedTxBytes = Convert.FromBase64String(unsignedTxB64);
    var unsignedTx = Algorand.Utils.Encoder.DecodeFromMsgPack<Algorand.Algod.Model.Transactions.Transaction>(unsignedTxBytes);
    var signedTx = unsignedTx.Sign(account);
    // Submit signedTx with your Algod client, e.g. algodApiInstance.RawTransactionAsync(...)
}
```

## Live router statistics

Useful for health checks / dashboards.

```csharp
var stats = await client.Api.StatsAsync();

Console.WriteLine($"Instance {stats.InstanceId} up for {stats.UptimeSeconds:N0}s, tracking {stats.PoolsCount} pool(s)");
```

## Export a snapshot of pools

```csharp
var snapshot = await client.Api.SnapshotAsync();

Console.WriteLine($"Snapshot generated at {snapshot.GeneratedAt:u} with {snapshot.Pools.Count} pool(s)");
```

## Error handling

All generated API calls throw `BiatecRouterConnector.Generated.BiatecRouterApiException` on non-success HTTP responses, which exposes the `StatusCode` and raw response body:

```csharp
try
{
    var outputAmount = await client.Api.QuoteAsync(fromAsset: 0, toAsset: 31566704, amount: 1_000_000);
}
catch (BiatecRouterConnector.Generated.BiatecRouterApiException ex)
{
    Console.WriteLine($"Router call failed with status {ex.StatusCode}: {ex.Response}");
}
```

The REST client is generated from `openapi.swagger.json` during build.
