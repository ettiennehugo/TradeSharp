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
    public static bool InstrumentBarDataFilterParse = true; //debugging for instrument bar data filter start/end date parsing
    public static bool MassInstrumentDataExport = true;     //debugging of mass export instrument bar data 
    public static bool MassInstrumentDataImport = false;     //debugging of mass import instrument bar data
    public static bool MassInstrumentDataDownload = true;   //debugging of mass download instrument bar data 


  }
}
