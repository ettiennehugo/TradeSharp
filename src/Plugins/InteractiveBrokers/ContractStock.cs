namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Interactive Brokers contract for stock instruments.
  /// </summary>
  public class ContractStock : IBApi.Contract
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties
    public string LastTradeDateOrContractMonth { get; set; }
    public string TradingClass { get; set; }
    public string Cusip { get; set; }
    public string LongName { get; set; }
    public string StockType { get; set; }
    public string IssueDate { get; set; }
    public string LastTradeTime { get; set; }
    public string Ratings { get; set; }
    public string Category { get; set; }
    public string Subcategory { get; set; }
    public string Industry { get; set; }
    public string TimeZoneId { get; set; }
    public string TradingHours { get; set; }
    public string LiquidHours { get; set; }
    public string OrderTypes { get; set; }
    public string MarketName { get; set; }
    public string ValidExchanges { get; set; }
    public string Notes { get; set; }

    //methods


  }
}
