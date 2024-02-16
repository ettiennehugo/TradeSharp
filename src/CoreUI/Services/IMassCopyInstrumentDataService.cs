using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface for the mass copy instrument data service.
  /// </summary>
  public interface IMassCopyInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    ILogger? Logger { get; set; }
    MassCopySettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task StartAsync(IProgressDialog progressDialog);
  }
}
