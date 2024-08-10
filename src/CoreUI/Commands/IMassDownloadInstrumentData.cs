using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.Commands
{
  /// <summary>
  /// Service to implement that mass download of instrument data.
  /// </summary>
  public interface IMassDownloadInstrumentData : ICommand
  {
    //constants


    //enums


    //types
    public struct Context
    {
      public string DataProvider;
      public MassDownloadSettings Settings;
      public IList<Instrument> Instruments;
    }

    //attributes


    //properties


    //methods


  }
}
