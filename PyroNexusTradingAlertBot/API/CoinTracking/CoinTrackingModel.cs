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