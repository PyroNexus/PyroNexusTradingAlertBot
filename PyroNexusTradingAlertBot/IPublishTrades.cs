using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot
{
    interface IPublishTrades
    {
        Task TradesTask(ulong discordChannelId, string[] blacklistedPairs);
    }
}