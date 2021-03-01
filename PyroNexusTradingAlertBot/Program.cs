using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using PyroNexusTradingAlertBot.API;
using PyroNexusTradingAlertBot.Storage;
using System.Net;
using System.Collections.Generic;
using System;
using System.Linq;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Configuration;

namespace PyroNexusTradingAlertBot
{
    class Program
    {
        private class Config
        {
            private IConfigurationRoot _configuration;
            public Config(string config) => _configuration = new ConfigurationBuilder()
                    .AddJsonFile(config)
                    .Build();

            public T GetConfig<T>() where T : new()
            {
                var obj = new T();
                _configuration.GetSection(typeof(T).Name.ToLower()).Bind(obj);
                return obj;
            }
        }

        static private ILogger<Program> _logger;

        private static Task LogDiscord(LogMessage msg)
        {
            _logger.LogInformation(msg.ToString());
            return Task.CompletedTask;
        }

        private static string getExchangeEmoji(string exchange)
        {
            switch (exchange)
            {
                case "Binance":
                    {
                        return "<:binance:815761052746121216>";
                    }
                case "Bitfinex":
                    {
                        return "<:bitfinex:815761128637988864>";
                    }
                case "Bittrex":
                    {
                        return "<:bittrex:815761128784789564>";
                    }
                case "CEX":
                    {
                        return "<:cex:815761128767356939>";
                    }
                case "Coinbase":
                case "Coinbase Pro":
                    {
                        return "<:coinbase:815761128704966668>";
                    }
                case "HitBTC":
                    {
                        return "<:hitbtc:815761128398913577>";
                    }
                case "KuCoin":
                    {
                        return "<:kucoin:815761128591196172>";
                    }
                default:
                    {
                        return "";
                    }
            }
        }

        static async Task Main(string[] args)
        {
            var config = new Config("Config.json");

            var globalConfig = config.GetConfig<GlobalConfig>();
            var coinTrackingConfig = config.GetConfig<CoinTrackingConfig>();
            var discordConfig = config.GetConfig<DiscordConfig>();
            var sqliteConfig = config.GetConfig<SqliteConfig>();

            var cookies = new CookieContainer();
            cookies.Add(new CookieCollection()
            {
                new Cookie("cointracking_cookie", coinTrackingConfig.Cookie1, "/import", ".cointracking.info"),
                new Cookie("cointracking_cookie2", coinTrackingConfig.Cookie2, "/import", ".cointracking.info")
            });
            var handler = new HttpClientHandler() { CookieContainer = cookies };

            var serviceCollection = new ServiceCollection()
                .AddOptions()
                .Configure<CoinTrackingOptions>(options => {
                    options.client = new HttpClient(handler);
                    options.key = coinTrackingConfig.ApiKey;
                    options.secret = coinTrackingConfig.ApiSecret;
                    options.updateJobs = coinTrackingConfig.UpdateJobs;
                })
                .Configure<SqliteOptions>(options =>
                {
                    options.DataSource = sqliteConfig.DatabaseFile;
                })
                .AddSingleton<ICoinTracking.LocalImportJobs, CoinTracking.LocalImportJobs>()
                .AddSingleton<ICoinTracking.RemoteUpdateJobs, CoinTracking.RemoteUpdateJobs>()
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

            var discord = new DiscordSocketClient();

            discord.Log += LogDiscord;
            await discord.LoginAsync(TokenType.Bot, discordConfig.BotToken);
            await discord.StartAsync();

            Task getTradesTask = new Task(async () =>
            {
                var discordChannel = discord.GetChannel(discordConfig.ChannelId) as SocketTextChannel;

                while (true)
                {
                    await serviceCollection.GetService<ICoinTracking.RemoteUpdateJobs>().UpdateTrades();
                    // Give cointracking a bit of time to process the new trades so they will hopefully be available via their API...
                    await Task.Delay(new TimeSpan(0, 5, 0));

                    await serviceCollection.GetService<ICoinTracking.LocalImportJobs>().GetTrades().ContinueWith(
                        trades =>
                        {
                            serviceCollection.GetService<ISqlite>().InsertTrades(trades.Result);
                        });

                    List<DbTrade> unpublishedTrades = new List<DbTrade>();

                    serviceCollection.GetService<ISqlite>().GetTradesNotPublishedToDiscord(unpublishedTrades).Wait();
                    if (unpublishedTrades.Any())
                    {
                        _logger.LogInformation("Posting unpublished trades to discord.");
                        foreach (DbTrade trade in unpublishedTrades)
                        {
                            if (globalConfig.BlacklistedPairs.Any(bp => bp == trade.buy_currency) && globalConfig.BlacklistedPairs.Any(bp => bp == trade.sell_currency)) {
                                _logger.LogDebug("Skipping trade because both pairs are blacklisted: {0} & {1}", trade.buy_currency, trade.sell_currency);
                                serviceCollection.GetService<ISqlite>().SetTradeIsIgnored(trade.cointracking_id);
                                continue;
                            }

                            int tries = 0;
                            int maxTries = 3;
                            var msg = "T: {0}. Exchange: {1} {10}. Type: {2}.\nBuy: {3} {4}.\nSell: {5} {6}.\nFee: {7} {8}.\nRate (buy / sell): {9} {4}/{6}";
                            var tradeTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                            tradeTime = tradeTime.AddSeconds(Convert.ToDouble(trade.time)).ToLocalTime();
                            var rate = Convert.ToDouble(trade.buy_amount) / Convert.ToDouble(trade.sell_amount);
                            var exchangeEmoji = getExchangeEmoji(trade.exchange);

                            msg = string.Format(msg, tradeTime,
                                trade.exchange,
                                trade.type,
                                trade.buy_amount,
                                trade.buy_currency,
                                trade.sell_amount,
                                trade.sell_currency,
                                trade.fee_amount,
                                trade.fee_currency,
                                rate,
                                exchangeEmoji);

                            IL_1:

                            bool messagePosted = false;

                            await discordChannel.SendMessageAsync(msg).ContinueWith(task =>
                            {
                                tries += 1;
                                if (!task.IsFaulted)
                                {
                                    serviceCollection.GetService<ISqlite>().SetTradeIsPublishedToDiscord(trade.cointracking_id);
                                    messagePosted = true;
                                }
                            });

                            if (!messagePosted)
                            {
                                if (tries > maxTries)
                                {
                                    throw new Exception("Failed to post to discord too many times");
                                }
                                _logger.LogDebug("Message failed to post to discord... retrying...");
                                await Task.Delay(5000);
                                goto IL_1;
                            }
                            
                        }
                    }

                    _logger.LogInformation("Finished posting trades to discord, waiting 180 mins for next run");
                    await Task.Delay(new TimeSpan(0, 180, 0));
                }
            });

            bool discordIsReady = false;

            discord.Ready += () => {
                discordIsReady = true;
                return Task.CompletedTask;
            };

            while (true)
            {
                await Task.Delay(new TimeSpan(0, 0, 1));
                if (discordIsReady)
                {
                    if (getTradesTask.Status == TaskStatus.Created)
                    {
                        var cw = getTradesTask.ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                var exception = new Exception("Exception running update task");
                                _logger.LogError(exception, "Exception running update task");
                                throw exception;
                            }
                        });
                        getTradesTask.Start();
                    }
                    break;
                }
            }

            await Task.Delay(-1);
        }
    }
}
