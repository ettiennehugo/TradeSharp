using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Base class for Interactive Brokers contracts - https://ibkrcampus.com/ibkr-api-page/contracts/
  /// </summary>
  public class Contract
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties
    public int ConId { get; set; }
    public string Symbol { get; set; }
    public string SecType { get; set; }
    public string SecId { get; set; }
    public string SecIdType { get; set; }
    public string Exchange { get; set; }
    public string Currency { get; set; }
    public string LocalSymbol { get; set; }
    public string PrimaryExchange { get; set; }

    //methods
    public InstrumentType InstrumentType()
    {
      if (SecType == IBApiAdapter.ContractTypeStock)
        return Data.InstrumentType.Stock;
      if (SecType == IBApiAdapter.ContractTypeFuture)
        return Data.InstrumentType.Future;
      if (SecType == IBApiAdapter.ContractTypeOption)
        return Data.InstrumentType.Option;
      if (SecType == IBApiAdapter.ContractTypeIndex)
        return Data.InstrumentType.Index;
      if (SecType == IBApiAdapter.ContractTypeForex)
        return Data.InstrumentType.Forex;
      if (SecType == IBApiAdapter.ContractTypeMutualFund)
        return Data.InstrumentType.MutualFund;

      return Data.InstrumentType.NotSupported;
    }
  }
}
