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
using Microsoft.Extensions.Configuration;

namespace PyroNexusTradingAlertBot
{
    class Program
    {
        private sealed class Config
        {
            private IConfigurationRoot _configuration;
            public Config(string config) => _configuration = new ConfigurationBuilder()
                    .AddJsonFile(config)
                    .Build();

            private T Get<T>() where T : class, new()
            {
                var obj = new T();
                _configuration.GetSection(typeof(T).Name).Bind(obj);
                return obj;
            }

            public GlobalConfig Global => Get<GlobalConfig>();
            public CoinTrackingConfig CoinTracking => Get<CoinTrackingConfig>();
            public DiscordConfig Discord => Get<DiscordConfig>();
            public SqliteConfig Sqlite => Get<SqliteConfig>();
        }

        private static ILogger<Program> _logger;
        private static ServiceProvider _services;
        private static Config _config;

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
            _config = new Config("Config.json");

            _services = new ServiceCollection()
                .AddOptions()
                .Configure<CoinTrackingOptions>(options => {
                    options.client = CoinTracking.GetClient(_config.CoinTracking.Cookie1, _config.CoinTracking.Cookie2);
                    options.key = _config.CoinTracking.ApiKey;
                    options.secret = _config.CoinTracking.ApiSecret;
                    options.updateJobs = _config.CoinTracking.UpdateJobs;
                })
                .Configure<SqliteOptions>(options =>
                {
                    options.DataSource = _config.Sqlite.DatabaseFile;
                })
                .Configure<DiscordServiceOptions>(options =>
                {
                    options.BotToken = _config.Discord.BotToken;
                })
                .AddSingleton<ICoinTracking.LocalImportJobs, CoinTracking.LocalImportJobs>()
                .AddSingleton<ICoinTracking.RemoteUpdateJobs, CoinTracking.RemoteUpdateJobs>()
                .AddSingleton<ISqlite, Sqlite>()
                .AddSingleton<IDiscordService, DiscordService>()
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

            _logger = _services.GetService<ILoggerFactory>().CreateLogger<Program>();
            _logger.LogInformation("Starting up...");

            Task buildSchema = _services.GetService<ISqlite>().BuildSchema();
            Task discordReady = _services.GetService<IDiscordService>().Ready();

            await buildSchema;
            await discordReady;

            await TradesTask(_config.Discord.ChannelId);

            await Task.Delay(-1);
        }

        private static async Task TradesTask(ulong discordChannelId)
        {
            while (true)
            {
                await _services.GetService<ICoinTracking.RemoteUpdateJobs>().UpdateTrades();
                // Give cointracking a bit of time to process the new trades so they will hopefully be available via their API...
                await Task.Delay(new TimeSpan(0, 5, 0));

                await _services.GetService<ICoinTracking.LocalImportJobs>().GetTrades().ContinueWith(
                    trades =>
                    {
                        _services.GetService<ISqlite>().InsertTrades(trades.Result);
                    });

                List<DbTrade> unpublishedTrades = new List<DbTrade>();

                await _services.GetService<ISqlite>().GetTradesNotPublishedToDiscord(unpublishedTrades);
                if (unpublishedTrades.Any())
                {
                    _logger.LogInformation("Posting unpublished trades to discord.");
                    await PublishTrades(discordChannelId, unpublishedTrades);
                    _logger.LogInformation("Finished posting trades to discord");
                }

                _logger.LogInformation("Waiting 360 mins for next run");
                await Task.Delay(new TimeSpan(0, 360, 0));
            }
        }

        private static async Task PublishTrades(ulong discordChannelId, List<DbTrade> unpublishedTrades)
        {
            var channel = _services.GetService<IDiscordService>().GetSocketTextChannel(discordChannelId);

            foreach (DbTrade trade in unpublishedTrades)
            {
                if (_config.Global.BlacklistedPairs.Any(bp => bp == trade.buy_currency) && _config.Global.BlacklistedPairs.Any(bp => bp == trade.sell_currency))
                {
                    _logger.LogDebug("Skipping trade because both pairs are blacklisted: {0} & {1}", trade.buy_currency, trade.sell_currency);
                    await _services.GetService<ISqlite>().SetTradeIsIgnored(trade.cointracking_id);
                    continue;
                }

                int tries = 0;
                int maxTries = 3;
                var msg = "T: {0}. Exchange: {1} {10}. Type: {2}.\nBuy: {3} {4}.\nSell: {5} {6}.\nFee: {7} {8}.\nRate (buy / sell): {9} {4}/{6}";
                var tradeTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                tradeTime = tradeTime.AddSeconds(Convert.ToDouble(trade.time)).ToLocalTime();
                var rate = Convert.ToDouble(trade.buy_amount) / Convert.ToDouble(trade.sell_amount);
                var exchangeEmoji = getExchangeEmoji(trade.exchange);

                msg = string.Format(msg,
                    tradeTime,
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

                await channel.SendMessageAsync(msg).ContinueWith(task =>
                {
                    tries += 1;
                    if (!task.IsFaulted)
                    {
                        _services.GetService<ISqlite>().SetTradeIsPublishedToDiscord(trade.cointracking_id);
                        messagePosted = true;
                    }
                });

                if (!messagePosted)
                {
                    if (tries > maxTries)
                    {
                        _logger.LogError("Failed to post message to discord. Giving up.");
                        break;
                    }
                    _logger.LogDebug("Message failed to post to discord... retrying...");
                    await Task.Delay(5000);
                    goto IL_1;
                }

            }
        }
    }
}
