using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.Storage
{
    public interface ISqlite
    {
        public void BuildSchema();
        public void Dispose();

        public void SetTradeIsPublishedToDiscord(int coinTrackingTradeId);
        public void SetTradeIsIgnored(int coinTrackingTradeId);

        public Task GetTradesNotPublishedToDiscord(List<DbTrade> trades);
        public void InsertTrades(Dictionary<string, Trade> trades);
        public DbDataReader ExecuteReader(string commandText);
        public int ExecuteNonQuery(string commandText);
    }
}