using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using PyroNexusTradingAlertBot.API;
using PyroNexusTradingAlertBot.Storage;
using System.Net;
using System.Configuration;

namespace PyroNexusTradingAlertBot
{
    class Program
    {
        private class Settings
        {
            static private string Get(string settingName) => ConfigurationManager.AppSettings.Get(settingName);
            readonly public CookieCollection Cookies = new CookieCollection()
            {
                new Cookie("cointracking_cookie", Get("CoinTrackingCookie1"), "/import", ".cointracking.info"),
                new Cookie("cointracking_cookie2", Get("CoinTrackingCookie2"), "/import", ".cointracking.info")
            };
            readonly public string CoinTrackingApiKey = Get("CoinTrackingApiKey");
            readonly public string CoinTrackingApiSecret = Get("CoinTrackingApiSecret");
        }

        static private ILogger<Program> _logger;

        static async Task Main(string[] args)
        {
            var settings = new Settings();

            var cookies = new CookieContainer();
            cookies.Add(settings.Cookies);
            var handler = new HttpClientHandler() { CookieContainer = cookies };

            var serviceCollection = new ServiceCollection()
                .AddOptions()
                .Configure<CoinTrackingOptions>(options => {
                    options.client = new HttpClient(handler);
                    options.key = settings.CoinTrackingApiKey;
                    options.secret = settings.CoinTrackingApiSecret;
                })
                .Configure<SqliteOptions>(options =>
                {
                    options.DataSource = "PyroNexusTradingAlertBot.db";
                })
                .AddSingleton<ICoinTracking, CoinTracking>()
                .AddSingleton<ISqlite, Sqlite>()
                .AddLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                .BuildServiceProvider();

            _logger = serviceCollection.GetService<ILoggerFactory>().CreateLogger<Program>();

            _logger.LogInformation("Starting up...");

            serviceCollection.GetService<ISqlite>().BuildSchema();

            //await serviceCollection.GetService<ICoinTracking>().UpdateTrades();

            var trades = await serviceCollection.GetService<ICoinTracking>().GetTrades();

            serviceCollection.GetService<ISqlite>().InsertTrades(trades);

            //string cmdtext = "SELECT name FROM sqlite_master WHERE type='table' AND name='Trades';";
            //using (var reader = serviceCollection.GetService<ISqlite>().ExecuteReader(cmdtext)) { 
            //    while (await reader.ReadAsync())
            //    {
            //        var name = reader.GetString(0);
            //        Console.WriteLine(name);
            //    }
            //}

            //var api = await serviceCollection.GetService<ICoinTracking>().getTrades();

            //CoinTracking api = new CoinTracking(httpClient, "", "");
            //using (StreamWriter outputFile = new StreamWriter(Path.Combine(dir, file)))
            //{
            //    await outputFile.WriteAsync(await api.getTrades());
            //}

            //var trades = await api.getTrades();


        }
    }
}
