using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to support for the mass export of instrument data.
  /// </summary>
  public interface IMassExportInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    ILogger Logger { get; set; }
    MassExportSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task Start(CancellationToken cancellationToken = default(CancellationToken));
  }
}
