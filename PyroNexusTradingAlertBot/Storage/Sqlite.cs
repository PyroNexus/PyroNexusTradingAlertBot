using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PyroNexusTradingAlertBot.Storage
{
    public class SqliteOptions
    {
        public string DataSource { get; set; }
    }

    public class Sqlite : ISqlite, IDisposable
    {
        private class SchemaTable
        {
            public string Name;
            public string SchemaText;
        }

        private static class Schema
        {
            static public List<SchemaTable> Tables = new List<SchemaTable>
            {
                new SchemaTable(){ Name = "Trades", SchemaText = Properties.SQL.Trades }
            };
        }

        protected readonly ILogger _logger;
        protected SqliteConnection sqliteConnection;

        public Sqlite(SqliteConnection connection, ILogger<Sqlite> logger)
        {
            _logger = logger;
            sqliteConnection = connection;
            sqliteConnection.Open();
        }

        public Sqlite(string connectionString, ILogger<Sqlite> logger)
            : this(new SqliteConnection(connectionString), logger)
        { }

        public Sqlite(IOptions<SqliteOptions> options, ILogger<Sqlite> logger)
            : this(new SqliteConnectionStringBuilder() {DataSource = options.Value.DataSource}.ConnectionString, logger)
        { }

        public void Dispose()
        {
            if (sqliteConnection.State != System.Data.ConnectionState.Closed)
            {
                sqliteConnection.Close();
                sqliteConnection.Dispose();
            }
            if (sqliteConnection != null)
            {
                sqliteConnection = null;
            }
        }

        

        private string SanitizeQueryText(string queryString)
        {
            return queryString.Replace("\r", "").Replace("\n", "");
        }

        public void BuildSchema()
        {
            _logger.LogDebug("Building DB schema...");

            foreach (SchemaTable table in Schema.Tables)
            {
                if (TableExists(table.Name))
                {
                    _logger.LogDebug("Skipping creating table '{0}' because it already exists in the database.", table.Name);
                    continue;
                }
                _logger.LogDebug("Building schema table '{0}' with SQL text:\r\n{1}", table.Name, table.SchemaText);
                ExecuteNonQuery(table.SchemaText);

                if (!TableExists(table.Name))
                {
                    var exception = new Exception("Schema was not created as expected");
                    _logger.LogCritical(exception, "Schema table with name '{0}' was not created", table.Name);
                    throw exception;
                }
            }
        }

        private void SetTradeColumnValue(string columnName, string columnValue, int coinTrackingTradeId)
        {
            var command = @"UPDATE Trades SET `{0}` = {1} WHERE cointracking_id = {2}";
            command = string.Format(command, columnName, columnValue, coinTrackingTradeId);
            var result = ExecuteNonQuery(command);
            if (result != 1)
            {
                var exception = new Exception("Expected 1 row to be updated!");
                _logger.LogError(exception, "Expected 1 row to be updated! Total rows updated: {0} trades updated for cointracking_id {1}", result, coinTrackingTradeId);
                throw exception;
            }
        }

        public void SetTradeIsPublishedToDiscord(int coinTrackingTradeId) => SetTradeColumnValue("is_published", "1", coinTrackingTradeId);
        public void SetTradeIsIgnored(int coinTrackingTradeId) => SetTradeColumnValue("is_ignored", "1", coinTrackingTradeId);

        public async Task GetTradesNotPublishedToDiscord(List<DbTrade> trades)
        {
            var dateFilter = DateTime.UtcNow.AddYears(-1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            var command = @"SELECT * FROM Trades WHERE is_published = 0 AND is_ignored = 0 AND type IN ('Trade', 'Margin Trade') AND time > {0} ORDER BY time ASC";
            command = string.Format(command, dateFilter);
            using (var reader = await ExecuteReaderAsync(command))
            {
                while (await reader.ReadAsync())
                {
                    trades.Add(CreateTrade(reader));
                }
            }
        }

        private DbTrade CreateTrade(SqliteDataReader reader) => new DbTrade()
        {
            buy_amount = reader["buy_amount"].ToString(),
            buy_currency = reader["buy_currency"].ToString(),
            comment = reader["comment"].ToString(),
            exchange = reader["exchange"].ToString(),
            fee_amount = reader["fee_amount"].ToString(),
            fee_currency = reader["fee_currency"].ToString(),
            imported_from = reader["imported_from"].ToString(),
            group = reader["group"].ToString(),
            imported_time = reader["imported_time"].ToString(),
            sell_amount = reader["sell_amount"].ToString(),
            sell_currency = reader["sell_currency"].ToString(),
            time = reader["time"].ToString(),
            trade_id = reader["trade_id"].ToString(),
            type = reader["type"].ToString(),
            is_published = Convert.ToInt32(reader["is_published"]),
            is_ignored = Convert.ToInt32(reader["is_ignored"]),
            cointracking_id = Convert.ToInt32(reader["cointracking_id"]),
        };

        private void Insert<T>(Dictionary<string, T> data, string tableName)
        {
            var props = typeof(T).GetProperties();
            string columns = "";
            foreach (var prop in props)
            {
                columns += string.Format("`{0}`,", prop.Name);
            }
            columns += "`cointracking_id`";

            foreach (var row in data)
            {
                var command = @"INSERT INTO {0} ({1}) SELECT {2} WHERE NOT EXISTS (SELECT 1 FROM {0} WHERE cointracking_id = {3});";
                string values = "";
                foreach (var prop in props)
                {
                    string value = (string)row.Value.GetType().GetProperty(prop.Name).GetValue(row.Value);
                    Type type = row.Value.GetType().GetProperty(prop.Name).GetValue(row.Value).GetType();

                    switch (type)
                    {
                        case Type stringType when stringType == typeof(string):
                            {
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    value = string.Format("'{0}',", value);
                                }
                                else
                                {
                                    value = "NULL,";
                                }
                                break;
                            }
                        case Type intType when intType == typeof(int):
                            {
                                value = string.Format("{0},", value);
                                break;
                            }
                        default:
                            {
                                var exception = new Exception("Unknown type");
                                _logger.LogCritical(exception, "An unsupported object type was used in the model: {0}", prop.Name);
                                throw exception;
                            }
                    }

                    values += value;
                }
                values += string.Format("{0}", row.Key);
                command = string.Format(command, tableName, columns, values, row.Key);
                ExecuteNonQuery(command);
            }
        }

        private bool TableExists(string tableName) => ExecuteReader(string.Format("SELECT name FROM sqlite_master WHERE type='table' AND name='{0}';", tableName)).HasRows;

        public virtual void InsertTrades(Dictionary<string, Trade> trades) => Insert(trades, "Trades");
        public virtual DbDataReader ExecuteReader(string commandText) => new SqliteCommand(SanitizeQueryText(commandText), sqliteConnection).ExecuteReader();
        public virtual int ExecuteNonQuery(string commandText) => new SqliteCommand(SanitizeQueryText(commandText), sqliteConnection).ExecuteNonQuery();

        public virtual Task<SqliteDataReader> ExecuteReaderAsync(string commandText) => new SqliteCommand(SanitizeQueryText(commandText), sqliteConnection).ExecuteReaderAsync();
    }
}
