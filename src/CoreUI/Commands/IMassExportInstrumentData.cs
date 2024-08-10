using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.Commands
{
  /// <summary>
  /// Interface to support for the mass export of instrument data.
  /// </summary>
  public interface IMassExportInstrumentData : ICommand
  {
    //constants
    public const string TokenMinute = "minute";
    public const string TokenHour = "hour";
    public const string TokenDay = "day";
    public const string TokenWeek = "week";
    public const string TokenMonth = "month";

    //enums


    //types
    public struct Context
    {
      public string DataProvider;
      public MassExportSettings Settings;
      public IList<Instrument> Instruments;
    }

    //attributes


    //properties


    //methods


  }
}
