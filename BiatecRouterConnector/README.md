# BiatecRouterConnector

NuGet library for calling the Biatec Router API (routing swaps and generating transactions to be signed on Algorand / AVM chains).

## Usage

The API uses an `Authorization` header carrying an ARC-0014 authentication transaction. The snippet below mirrors the working integration test.

> IMPORTANT: `ReceiveMinimum` is your slippage protection.
> Setting it too low (e.g. `1`, as in the example) effectively disables protection and can result in receiving far less than expected.
> In production, compute `ReceiveMinimum` from your quoted output amount and an explicit slippage tolerance (and consider fees and decimal precision).

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

// 3) Call the router: request a route for ALGO (0) -> USDC (31566704).
var routerHttpClient = new HttpClient();
var client = new BiatecRouterClient(routerHttpClient, authorization: authHeader);

var result = await client.Api.RouteTxsAsync(new BiatecRouterConnector.Generated.RouteInputParameters
{
    FromAsset = 0,
    ToAsset = 31566704,
    SwapAmount = 1_000_000, // 1 ALGO
    ReceiveMinimum = 1, // WARNING: example only; do NOT use this value in production.
    Sender = account.Address.ToString(),
    TransParams = txparams.ToRouterParams()
});

// result.Routes[0].TxsToSign contains unsigned transactions to be signed and submitted.
```

The REST client is generated from `openapi.swagger.json` during build.
