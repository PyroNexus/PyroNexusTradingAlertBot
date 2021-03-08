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
using Bitfinex.Net.Objects.RestV1Objects;
using PyroNexusTradingAlertBot.Storage;
using PyroNexusTradingAlertBot.Storage.Model;

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

        public async Task Get<M>(M symbols) where M : class
        {
            switch (typeof(M))
            {
                case Type Symbol when Symbol == typeof(Symbols):
                    await GetSymbols(symbols as Symbols);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task GetSymbols(Symbols symbols)
        {
            using var client = new BitfinexClient();

            var data = await client.GetSymbolDetailsAsync();
            foreach (var symbol in data.Data)
            {
                symbols.AllSymbols.Add(new Symbol(symbol));
            }
        }


    }
}
