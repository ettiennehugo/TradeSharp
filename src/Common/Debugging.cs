namespace TradeSharp.Common
{
  /// <summary>
  /// Debug settings.
  /// </summary>
  public class Debugging
  {
    // General debugging switches
    public static bool DatabaseCalls = false;   //log database calls, e.g. SQL queries
    public static bool ImportExport = false;    //log debug messages around import/export functionality
    public static bool Copy = true;             //log messages for all copy operations

    // Instrument Bar Data debugging switches
    public static bool InstrumentBarDataLoadAsync = true;   //debugging for instrument bar data LoadAsync
    public static bool InstrumentBarDataFilterParse = true;   //debugging for instrument bar data filter start/end date parsing

  }
}
