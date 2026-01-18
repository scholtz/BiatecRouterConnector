# BiatecRouterConnector

NuGet library for calling the Biatec Router (AVM Trade Reporter) REST API.

## Usage

```csharp
using BiatecRouterConnector;

var client = BiatecRouterClient.Create(new BiatecRouterClientOptions
{
    // The API uses an API-key style header named Authorization (ARC-0014 token)
    Authorization = "YOUR_ARC0014_AUTH_VALUE"
});

// Call generated API methods via client.Api
var pools = await client.Api.ApiPoolAsync(assetIdA: 0, assetIdB: 0, address: null, protocol: null, size: 100);
```

The REST client is generated from `openapi.swagger.json` during build.
