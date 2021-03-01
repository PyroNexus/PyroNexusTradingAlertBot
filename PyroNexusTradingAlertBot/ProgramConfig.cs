using System;
using System.Collections.Generic;
using System.Text;

namespace PyroNexusTradingAlertBot
{
    public class GlobalConfig
    {
        public string[] BlacklistedPairs { get; set; }
    }

    public class SqliteConfig
    {
        public string DatabaseFile { get; set; }
    }
    public class CoinTrackingConfig
    {
        public string Cookie1 { get; set; }
        public string Cookie2 { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public List<CoinTrackingUpdateJob> UpdateJobs { get; set; }
    }

    public class CoinTrackingUpdateJob
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int[] JobIds { get; set; }
    }

    public class DiscordConfig
    {
        public string BotToken { get; set; }
        public ulong ChannelId { get; set; }
    }
}
