using System.Net;
using Algorand;
using Algorand.Algod;
using BiatecRouterConnector;

namespace BiatectRouterConnectorTests;

public sealed class BiatecRouterClientTests
{
    [Test]
    public async Task WhenRequestingTradesForAlgoToUsdcThenResponseIsReturned()
    {
        // Arrange
        // This is an integration-style test:
        // 1) It talks to Algorand MainNet ALGOd to fetch current transaction parameters.
        // 2) It uses those parameters to create an ARC-0014-compatible auth payload.
        // 3) It calls the Biatec Router endpoint to get a route and unsigned txns.

        // Configure ALGOd client for Algorand MainNet.
        using var algodHttpClient = HttpClientConfigurator.ConfigureHttpClient(AlgodConfiguration.MainNet);
        DefaultApi algodApiInstance = new DefaultApi(algodHttpClient);

        // NOTE: Replace this mnemonic with a real one only in your local environment.
        // Do not commit secrets. The router only needs it to produce a signed auth payload.
        var account = AlgorandARC76AccountDotNet.ARC76.GetAccount("test test test test test test test test test test test test test test test test test test test test test test test test");

        // The router API expects Authorization header value containing an ARC-0014 auth transaction.
        var routerHttpClient = new HttpClient();

        // Fetch suggested network transaction parameters.
        var txparams = await algodApiInstance.TransactionParamsAsync();

        // We sign a 0-ALGO self-payment with note "BiatecRouter#ARC14" to prove account ownership.
        // This produces the ARC-0014 auth payload (base64-encoded signed transaction bytes).
        txparams.Fee = 0;
        var tx = Algorand.Algod.Model.Transactions.PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account.Address, account.Address, 0, "BiatecRouter#ARC14", txparams);
        var signed = tx.Sign(account);
        var authHeader = Convert.ToBase64String(Algorand.Utils.Encoder.EncodeToMsgPackOrdered(signed));

        // System under test: REST client wrapper with the Authorization header.
        var sut = new BiatecRouterClient(routerHttpClient, authorization: authHeader);

        try
        {
            // Act
            // Request a route + a list of transactions to sign for swapping 1 ALGO -> USDC.
            var response = await sut.Api.RouteTxsAsync(new BiatecRouterConnector.Generated.RouteInputParameters()
            {
                FromAsset = 0, // ALGO
                ToAsset = 31566704, // USDC
                SwapAmount = 1_000_000, // 1 ALGO
                ReceiveMinimum = 1,
                Sender = account.Address.ToString(),
                TransParams = txparams.ToRouterParams()
            });

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Routes, Is.Not.Null);
            Assert.That(response.Routes.First(), Is.Not.Null);
            Assert.That(response.Routes.First().TxsToSign, Is.Not.Null);
            Assert.That(response.Routes.First().Route.OutputAmount, Is.GreaterThan(1));
            Assert.That(response.Routes.First().TxsToSign.Count, Is.GreaterThan(1));
        }
        catch (BiatecRouterConnector.Generated.BiatecRouterApiException ex) when (ex.StatusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden)
        {
            Assert.Ignore("Endpoint requires authorization in this environment.");
        }
        catch (HttpRequestException)
        {
            Assert.Ignore("Public API is not reachable from the test environment.");
        }
    }
}
