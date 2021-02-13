using System;
using System.Collections.Generic;
using System.Data.Common;
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
            : this (new SqliteConnectionStringBuilder() {DataSource = options.Value.DataSource}.ConnectionString, logger)
        { }

        public void Dispose()
        {
            if (sqliteConnection.State != System.Data.ConnectionState.Closed || sqliteConnection != null)
            {
                sqliteConnection.Close();
                sqliteConnection.Dispose();
                sqliteConnection = null;
            }
        }

        private bool TableExists(string tableName)
        {
            using (var reader = ExecuteReader(string.Format("SELECT name FROM sqlite_master WHERE type='table' AND name='{0}';", tableName)))
            {
                if (reader.HasRows)
                {
                    return true;
                }
                return false;
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

        public virtual void Insert<T>(Dictionary<string, T> data, string tableName)
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

        public virtual void InsertTrades(Dictionary<string, Trade> trades) => Insert(trades, "Trades");
        public virtual DbDataReader ExecuteReader(string commandText) => new SqliteCommand(SanitizeQueryText(commandText), sqliteConnection).ExecuteReader();
        public virtual int ExecuteNonQuery(string commandText) => new SqliteCommand(SanitizeQueryText(commandText), sqliteConnection).ExecuteNonQuery();
    }
}
