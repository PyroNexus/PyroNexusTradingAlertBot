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

    public class RavenDBConfig
    {
        public string[] ServerUrls { get; set; }
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

    public class CoinTrackingUpdateJob
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int[] JobIds { get; set; }
    }
}
