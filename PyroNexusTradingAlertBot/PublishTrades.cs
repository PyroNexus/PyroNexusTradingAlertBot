using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PyroNexusTradingAlertBot.API;
using PyroNexusTradingAlertBot.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot
{
    public class PublishTradesOptions
    {
        public ICoinTrackingService.LocalImport LocalImportService;
        public IDiscordService DiscordService;
        public ISqliteService SqliteService;
        public int RefreshInterval;
    }
    class PublishTrades : IPublishTrades
    {
        readonly ICoinTrackingService.LocalImport _localImport;
        readonly IDiscordService _discord;
        readonly ISqliteService _sqlite;
        readonly ILogger _logger;
        readonly int _refreshInterval;

        public PublishTrades(IOptions<PublishTradesOptions> options, ILogger<PublishTrades> logger)
        {
            _logger = logger;
            _localImport = options.Value.LocalImportService;
            _discord = options.Value.DiscordService;
            _sqlite = options.Value.SqliteService;
            _refreshInterval = options.Value.RefreshInterval;
        }

        private static string GetExchangeEmoji(string exchange)
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

        public async Task TradesTask(ulong discordChannelId, string[] blacklistedPairs)
        {
            var oneYearAgo = DateTime.UtcNow.AddYears(-1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var twoYearsAgo = DateTime.UtcNow.AddYears(-2).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            while (true)
            {
                await _localImport.GetTrades(start: (int)twoYearsAgo).ContinueWith(
                    trades =>
                    {
                        _sqlite.InsertTrades(trades.Result);
                    });

                List<DbTrade> unpublishedTrades = new List<DbTrade>();

                await _sqlite.GetTradesNotPublishedToDiscord(unpublishedTrades, oneYearAgo);
                if (unpublishedTrades.Any())
                {
                    _logger.LogInformation("Posting unpublished trades to discord.");
                    await Publish(discordChannelId, unpublishedTrades, blacklistedPairs);
                    _logger.LogInformation("Finished posting trades to discord");
                }

                _logger.LogInformation("Waiting {0} hours for next run", _refreshInterval);
                await Task.Delay(new TimeSpan(_refreshInterval, 0, 0));
            }
        }

        private async Task Publish(ulong discordChannelId, List<DbTrade> unpublishedTrades, string[] blacklistedPairs)
        {
            var channel = _discord.GetSocketTextChannel(discordChannelId);

            foreach (DbTrade trade in unpublishedTrades)
            {
                if (blacklistedPairs.Any(bp => bp == trade.buy_currency) && blacklistedPairs.Any(bp => bp == trade.sell_currency))
                {
                    _logger.LogDebug("Skipping trade because both pairs are blacklisted: {0} & {1}", trade.buy_currency, trade.sell_currency);
                    await _sqlite.SetTradeIsIgnored(trade.cointracking_id);
                    continue;
                }

                int tries = 0;
                int maxTries = 3;
                var msg = "T: {0}. Exchange: {1} {10}. Type: {2}.\nBuy: {3} {4}.\nSell: {5} {6}.\nFee: {7} {8}.\nRate (buy / sell): {9} {4}/{6}";
                var tradeTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                tradeTime = tradeTime.AddSeconds(Convert.ToDouble(trade.time)).ToLocalTime();
                var rate = Convert.ToDouble(trade.buy_amount) / Convert.ToDouble(trade.sell_amount);
                var exchangeEmoji = GetExchangeEmoji(trade.exchange);

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
                        _sqlite.SetTradeIsPublished(trade.cointracking_id);
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
