using Microsoft.Extensions.Logging;
using IBApi;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Copy the Interactive Brokers contract definitions to the TradeSharp Instruments.
  /// </summary>
  public class CopyContractsToInstruments
  {
    //constants


    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;

    //constructors
    public CopyContractsToInstruments(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {

      //TODO - Do not know whether this would be possible since we already download the contracts from IB based
      //       on the defined set of Instrument definitions due to the fact that the search does not return all
      //       the defined instruments.
      m_adapter.m_logger.LogCritical("Copy contracts to instruments not implemented.");

    }
  }
}
