using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.Commands
{
  /// <summary>
  /// Interface for the mass copy instrument data command.
  /// </summary>
  public interface IMassCopyInstrumentData : ICommand
  {
    //constants


    //enums


    //types
    /// <summary>
    /// Required context for the command.
    /// </summary>
    public struct Context
    {
      public string DataProvider;
      public MassCopySettings Settings;
      public IList<Instrument> Instruments;
    }

    //attributes


    //properties


    //methods


  }
}
