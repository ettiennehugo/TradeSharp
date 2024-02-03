using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI.Services
{
  /// <summary> 
  /// Implementation of the mass download of instrument data.
  /// </summary>
  public class MassDownloadInstrumentDataService : IMassDownloadInstrumentDataService
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
    public MassDownloadSettings Settings { get; set; }
    public bool IsRunning { get; internal set; }

    //methods
    public Task Start(CancellationToken cancellationToken = default)
    {
      throw new NotImplementedException();
    }
  }
}
