using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.WinCoreUI.Services
{
  /// <summary>
  /// Windows implementation of the mass import of instrument data - use as singleton.
  /// </summary>
  public class MassImportInstrumentDataService : IMassImportInstrumentDataService
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
    public MassImportSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task IMassImportInstrumentDataService.Start(CancellationToken cancellationToken)
    { 
      throw new NotImplementedException();
    }
  }
}
