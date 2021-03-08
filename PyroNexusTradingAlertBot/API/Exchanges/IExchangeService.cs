using PyroNexusTradingAlertBot.Storage;
using PyroNexusTradingAlertBot.Storage.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.API.Exchanges
{
    interface IExchangeService
    {
        public Task Get<M>(M symbols) where M : class;

        //public Task GetSymbols(List<Symbol> symbols);
    }
}
