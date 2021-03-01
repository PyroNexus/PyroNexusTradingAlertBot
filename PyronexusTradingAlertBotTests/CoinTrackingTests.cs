using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using PyroNexusTradingAlertBot.API;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace PyronexusTradingAlertBotTests
{
    public class SetupCointrackingService: IDisposable
    {
        private readonly FileStream fs;
        public CoinTracking.LocalImportJobs cointracking;
        public SetupCointrackingService(string testDataFilename)
        {
            var mock = new Mock<HttpMessageHandler>();

            fs = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", testDataFilename + ".json"));

            var response = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StreamContent(fs)
            };

            mock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

            IOptions<CoinTrackingOptions> coinTrackingOptions = Options.Create(new CoinTrackingOptions()
            {
                client = new HttpClient(mock.Object),
                key = "a",
                secret = "b"
            });

            cointracking = new CoinTracking.LocalImportJobs(coinTrackingOptions, null);
        }
        public void Dispose()
        {
            cointracking = null;
            if (fs != null && fs is IDisposable)
            {
                fs.Dispose();
            }
        }
    }

    [TestClass]
    public class CoinTrackingTests
    {
        [TestMethod]
        public async Task TestGetTrades()
        {
            using (var service = new SetupCointrackingService("Trade"))
            {
                var trades = await service.cointracking.GetTrades();
                Assert.AreEqual(trades.Single(t => t.Key == "100604399").Value.time, "1502697000");
            }
        }
    }
}
