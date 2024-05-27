using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Service to implement that mass download of instrument data.
  /// </summary>
  public interface IMassDownloadInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IDataProviderPlugin DataProvider { get; set; }
    ILogger? Logger { get; set; }
    MassDownloadSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task StartAsync(IProgressDialog progressDialog, IList<Instrument> instruments);
  }
}
