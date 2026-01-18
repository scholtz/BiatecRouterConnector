using Algorand;
using Algorand.Algod;
using BiatecRouterConnector;
using System.Net;

namespace BiatectRouterConnectorTests;

public sealed class BiatecRouterClientTests
{
    [Test]
    public async Task WhenRequestingTradesForAlgoToUsdcThenResponseIsReturned()
    {
        var routerHttpClient = new HttpClient
        {
            BaseAddress = new Uri("https://router.api.biatec.io")
        };

        var sut = new BiatecRouterClient(routerHttpClient, authorization: null);

        try
        {
            using var algodHttpClient = HttpClientConfigurator.ConfigureHttpClient(AlgodConfiguration.MainNet);
            DefaultApi algodApiInstance = new DefaultApi(algodHttpClient);


            var account = AlgorandARC76AccountDotNet.ARC76.GetAccount("test test test test test test test test test test test test test test test test test test test test test test test test");
            var txparams = await algodApiInstance.TransactionParamsAsync();
            txparams.Fee = 0;
            var tx = Algorand.Algod.Model.Transactions.PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account.Address, account.Address, 0, "BiatecRouter#ARC14", txparams);
            var signed = tx.Sign(account);
            //var authHeader = $"Tnx {Convert.ToBase64String(Algorand.Utils.Encoder.EncodeToMsgPackOrdered(signed))}";
            routerHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SigTx", Convert.ToBase64String(Algorand.Utils.Encoder.EncodeToMsgPackOrdered(signed)));
            var response = await sut.Api.RouteAsync(0, 31566704, 1000000);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.First().OutputAmount, Is.GreaterThan(1));
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
