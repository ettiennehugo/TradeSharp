using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Windows implementation of the mass export of instrument data - use as singleton.
  /// </summary>
  public class MassExportInstrumentDataService : IMassExportInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties
    public ILogger Logger { get; set; }
    public MassExportSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    public Task Start(CancellationToken cancellationToken = default)
    {
      throw new System.NotImplementedException();
    }
  }
}
