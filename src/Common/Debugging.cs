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
    public static bool InstrumentBarDataLoadAsync = false;   //debugging for instrument bar data LoadAsync
    public static bool InstrumentBarDataFilterParse = false; //debugging for instrument bar data filter start/end date parsing
    public static bool MassInstrumentDataExport = false;     //debugging of mass export instrument bar data 
    public static bool MassInstrumentDataImport = false;     //debugging of mass import instrument bar data
    public static bool MassInstrumentDataImportException = false;     //debugging of mass import instrument bar data exceptions
    public static bool MassInstrumentDataCopy = true;   //debugging of mass copy instrument bar data 
    public static bool MassInstrumentDataDownload = false;   //debugging of mass download instrument bar data 


  }
}
