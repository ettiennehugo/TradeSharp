using Microsoft.Extensions.Logging;

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
    ILogger Logger { get; set; }
    MassDownloadSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task Start(CancellationToken cancellationToken = default(CancellationToken));

  }
}
