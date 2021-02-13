using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data.Common;

namespace PyroNexusTradingAlertBot.Storage
{
    public interface ISqlite
    {
        public void BuildSchema();
        public void Dispose();

        public void InsertTrades(Dictionary<string, Trade> trades);
        public DbDataReader ExecuteReader(string commandText);
        public int ExecuteNonQuery(string commandText);
    }
}