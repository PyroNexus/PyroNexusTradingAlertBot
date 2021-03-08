using Binance.Net.Objects.Spot.MarketData;
using Bitfinex.Net.Objects.RestV1Objects;
using CryptoExchange.Net.ExchangeInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.Storage.Model
{
    internal class Symbols
    {
        internal List<Symbol> AllSymbols { get; set; }

        internal Symbols()
        {
            AllSymbols = new List<Symbol>();
        }

        internal Symbols(List<Symbol> allSymbols)
        {
            AllSymbols = allSymbols;
        }

    }

    public class Symbol
    {
        internal string Name { get; private set; }
        internal bool MarginEnabled { get; private set; }
        internal bool SpotEnabled { get; private set; }

        internal Symbol() { }
        internal Symbol(ICommonSymbol symbol)
        {
            Name = symbol.CommonName;
            
            if (symbol is BitfinexSymbolDetails bsd)
            {
                MarginEnabled = bsd.Margin;
            }

            if (symbol is BinanceSymbol bs)
            {
                SpotEnabled = bs.IsSpotTradingAllowed;
                MarginEnabled = bs.IsMarginTradingAllowed;
            }
            
        }
    }
}
