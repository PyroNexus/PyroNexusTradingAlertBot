using Bitfinex.Net;
using Bitfinex.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PyroNexusTradingAlertBot.Helpers;

namespace PyroNexusTradingAlertBot.API.Exchanges
{
    public class BitfinexExchangeServiceOptions
    {
        public string Key { get; set; }
        public string Secret { get; set; }
    }
    public class BitfinexExchangeService : IBitfinexExchangeService
    {
        readonly ILogger _logger;
        
        public BitfinexExchangeService(IOptions<BitfinexExchangeServiceOptions> options, ILogger<BitfinexExchangeService> logger)
        {
            _logger = logger;

            CryptoExchangeHelper.TextWriterILogger textWriter = new CryptoExchangeHelper.TextWriterILogger(_logger);

            BitfinexClient.SetDefaultOptions(new BitfinexClientOptions()
            {
                ApiCredentials = new ApiCredentials(options.Value.Key, options.Value.Secret),
                LogVerbosity = CryptoExchangeHelper.GetLogVerbosity(textWriter.LogLevel),
                LogWriters = new List<TextWriter> { textWriter }
            });




        }

        public void GetCurrencies()
        {
            using var client = new BitfinexClient();

            var data = client.GetSymbolDetails();
            var d2 = client.GetOrderHistory("tLINKF0:tUSTF0");
        }


    }
}
