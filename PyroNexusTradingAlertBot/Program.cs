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
using Microsoft.Extensions.Options;

namespace PyroNexusTradingAlertBot
{
    class Program
    {
        private sealed class Config
        {
            private readonly IConfigurationRoot _configuration;
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

        private static ILogger _logger;
        private static IServiceProvider _services;
        private static Config _config;

        static async Task Main(string[] args)
        {
            _config = new Config("Config.json");

            _services = new ServiceCollection()
                .AddOptions()
                .Configure<CoinTrackingOptions>(options =>
                {
                    options.Client = CoinTrackingService.GetClient(_config.CoinTracking.Cookie1, _config.CoinTracking.Cookie2);
                    options.Key = _config.CoinTracking.ApiKey;
                    options.Secret = _config.CoinTracking.ApiSecret;
                    options.UpdateJobs = _config.CoinTracking.UpdateJobs;
                    options.UpdateJobsRefreshInterval = _config.CoinTracking.UpdateJobsRefreshInterval;
                })
                .Configure<SqliteOptions>(options =>
                {
                    options.DataSource = _config.Sqlite.DatabaseFile;
                })
                .Configure<DiscordServiceOptions>(options =>
                {
                    options.BotToken = _config.Discord.BotToken;
                })
                .AddSingleton<ICoinTrackingService.LocalImport, CoinTrackingService.LocalImport>()
                .AddSingleton<ICoinTrackingService.RemoteUpdate, CoinTrackingService.RemoteUpdate>()
                .AddSingleton<ISqliteService, SqliteService>()
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

            IOptions<PublishTradesOptions> publishTradesOptions = Options.Create(new PublishTradesOptions()
            {
                DiscordService = _services.GetService<IDiscordService>(),
                LocalImportService = _services.GetService<ICoinTrackingService.LocalImport>(),
                SqliteService = _services.GetService<ISqliteService>(),
                RefreshInterval = _config.Global.PublishTradesRefreshInterval
            });
            ILogger<PublishTrades> publishTradesLogger = _services.GetService<ILoggerFactory>().CreateLogger<PublishTrades>();

            _logger = _services.GetService<ILoggerFactory>().CreateLogger<Program>();
            _logger.LogInformation("Starting up...");

            Task buildSchema = _services.GetService<ISqliteService>().BuildSchema();
            Task discordReady = _services.GetService<IDiscordService>().Ready();
            Task remoteUpdateJobs = _services.GetService<ICoinTrackingService.RemoteUpdate>().UpdateTrades();

            await buildSchema;
            await discordReady;

            Task publishTrades = new PublishTrades(publishTradesOptions, publishTradesLogger)
                .TradesTask(_config.Discord.ChannelId, _config.Global.BlacklistedPairs);

            await Task.Delay(-1);
        }
    }
}
