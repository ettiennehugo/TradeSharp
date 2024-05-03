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

      //TODO
      m_adapter.m_logger.LogCritical("Copy contracts to instruments not implemented.");

    }
  }
}
