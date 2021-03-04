using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace PyroNexusTradingAlertBot
{
    public class Result
    {
        public int success { get; set; }
        public string method { get; set; }
        public string error { get; set; }
        public string error_msg { get; set; }
    }

    public class Trade
    {
        public string buy_amount { get; set; }
        public string buy_currency { get; set; }
        public string sell_amount { get; set; }
        public string sell_currency { get; set; }
        public string fee_amount { get; set; }
        public string fee_currency { get; set; }
        public string type { get; set; }
        public string exchange { get; set; }
        public string group { get; set; }
        public string comment { get; set; }
        public string imported_from { get; set; }
        public string time { get; set; }
        public string imported_time { get; set; }
        public string trade_id { get; set; }
    }

    public class DbTrade : Trade
    {
        public int is_published { get; set; }
        public int is_ignored { get; set; }
        public int cointracking_id { get; set; }

        public DbTrade(IDataReader reader)
        {
            buy_amount = reader["buy_amount"].ToString();
            buy_currency = reader["buy_currency"].ToString();
            comment = reader["comment"].ToString();
            exchange = reader["exchange"].ToString();
            fee_amount = reader["fee_amount"].ToString();
            fee_currency = reader["fee_currency"].ToString();
            imported_from = reader["imported_from"].ToString();
            group = reader["group"].ToString();
            imported_time = reader["imported_time"].ToString();
            sell_amount = reader["sell_amount"].ToString();
            sell_currency = reader["sell_currency"].ToString();
            time = reader["time"].ToString();
            trade_id = reader["trade_id"].ToString();
            type = reader["type"].ToString();
            is_published = Convert.ToInt32(reader["is_published"]);
            is_ignored = Convert.ToInt32(reader["is_ignored"]);
            cointracking_id = Convert.ToInt32(reader["cointracking_id"]);
        }
    }

    public class Balance
    {
        public string amount { get; set; }
        public string coin { get; set; }
        public string value_fiat { get; set; }
        public string value_btc { get; set; }
        public string price_fiat { get; set; }
        public string price_btc { get; set; }
        public string change1h { get; set; }
        public string change24h { get; set; }
        public string change7d { get; set; }
        public string change30d { get; set; }
    }
}