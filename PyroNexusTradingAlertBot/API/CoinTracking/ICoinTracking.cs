using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.API
{
    public interface ICoinTracking
    {
        public interface LocalImportJobs
        {
            Task<Dictionary<string, Trade>> GetTrades(int limit = 0, string order = "ASC", int start = 0, int end = 0, bool tradePrices = false);
        }

        public interface RemoteUpdateJobs
        {
            Task UpdateTrades();
        }
        
        //Task<Dictionary<string, Balance>> getBalance();
        //Task<string> getHistoricalSummary(bool btc, int start, int end);
        //Task<string> getHistoricalCurrency(string currency, int start, int end);
        //Task<string> getGroupedBalance(string group, bool excludeDepWith, string type);
        //Task<string> getGains(string price, bool btc);
    }
}