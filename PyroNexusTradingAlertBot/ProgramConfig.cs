using System;
using System.Collections.Generic;

namespace PyroNexusTradingAlertBot
{
    public class GlobalConfig
    {
        public string[] BlacklistedPairs { get; set; }

        private static int _minimumPublishTradesRefreshInterval = 1;
        private static int _defaultPublishTradesRefreshInterval = 2;
        private int _publishTradesRefreshInterval;
        public int PublishTradesRefreshInterval {
            get
            {
                switch (_publishTradesRefreshInterval)
                {
                    case 0:
                        return _defaultPublishTradesRefreshInterval;
                    case int number when number < _minimumPublishTradesRefreshInterval:
                        return _minimumPublishTradesRefreshInterval;
                    default:
                        return _publishTradesRefreshInterval;
                }
            }
            set
            {
                _publishTradesRefreshInterval = value;
            }
        }
    }

    public class SqliteConfig
    {
        public string DatabaseFile { get; set; }
    }

    public class BitfinexConfig
    {
        private string _key;
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = new Helpers.CryptoHelper().DecryptString(value);
            }
        }
        private string _secret;
        public string Secret
        {
            get
            {
                return _secret;
            }
            set
            {
                _secret = new Helpers.CryptoHelper().DecryptString(value);
            }
        }
    }

    public class CoinTrackingConfig
    {
        public string Cookie1 { get; set; }
        public string Cookie2 { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public List<CoinTrackingUpdateJob> UpdateJobs { get; set; }

        private static int _minimumUpdateJobsRefreshInterval = 1;
        private static int _defaultUpdateJobsRefreshInterval = 3;
        private int _updateJobsRefreshInterval;
        public int UpdateJobsRefreshInterval {
            get
            {
                switch (_updateJobsRefreshInterval)
                {
                    case 0:
                        return _defaultUpdateJobsRefreshInterval;
                    case int number when number < _minimumUpdateJobsRefreshInterval:
                        return _minimumUpdateJobsRefreshInterval;
                    default:
                        return _updateJobsRefreshInterval;
                }
            }
            set
            {
                _updateJobsRefreshInterval = value;
            }
        }
    }

    public class CoinTrackingUpdateJob
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int[] JobIds { get; set; }
    }

    public class DiscordConfig
    {
        private string _botToken;
        public string BotToken
        {
            get
            {
                return _botToken;
            }
            set
            {
                _botToken = new Helpers.CryptoHelper().DecryptString(value);
            }
        }
        public ulong ChannelId { get; set; }
    }
}
