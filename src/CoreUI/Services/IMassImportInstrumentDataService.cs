using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to support for the mass import of instrument data.
  /// </summary>
  public interface IMassImportInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    ILogger Logger { get; set; }
    MassImportSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task Start(CancellationToken cancellationToken = default(CancellationToken));

  }
}
