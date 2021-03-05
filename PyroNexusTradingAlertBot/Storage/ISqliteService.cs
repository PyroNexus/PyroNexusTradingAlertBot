using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.Storage
{
    public interface ISqliteService
    {
        public Task BuildSchema();
        public void Dispose();

        public Task SetTradeIsPublished(int coinTrackingTradeId);
        public Task SetTradeIsIgnored(int coinTrackingTradeId);

        public Task GetTradesNotPublishedToDiscord(List<DbTrade> trades, double dateFilter);
        public void InsertTrades(Dictionary<string, Trade> trades);
        public DbDataReader ExecuteReader(string commandText);
        public int ExecuteNonQuery(string commandText);
    }
}