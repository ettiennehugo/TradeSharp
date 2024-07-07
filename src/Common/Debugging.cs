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
    public static bool Copy = false;             //log messages for all copy operations

    //Instrument Group debugging switches
    public static bool InstrumentGroupImport = false;   //log messages for instrument group import
    public static bool InstrumentGroupExport = false;   //log messages for instrument group export
    public static bool InstrumentGroupService = false;  //log messages for instrument group service

    // Instrument Bar Data debugging switches
    public static bool InstrumentBarDataLoadAsync = false;   //debugging for instrument bar data LoadAsync
    public static bool InstrumentBarDataFilterParse = false; //debugging for instrument bar data filter start/end date parsing
    public static bool MassInstrumentDataExport = false;     //debugging of mass export instrument bar data 
    public static bool MassInstrumentDataImport = false;     //debugging of mass import instrument bar data
    public static bool MassInstrumentDataImportException = false;     //debugging of mass import instrument bar data exceptions
    public static bool MassInstrumentDataCopy = false;      //debugging of mass copy instrument bar data 
    public static bool MassInstrumentDataDownload = false;   //debugging of mass download instrument bar data 


  }
}
