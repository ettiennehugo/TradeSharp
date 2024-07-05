using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base implementation for data provider.
  /// </summary>
  [ComVisible(true)]
  [Guid("FD55CED4-9957-48DF-B65A-219449794B56")]
  public abstract class DataProviderPlugin : Plugin, IDataProviderPlugin
  {
    //constants


    //enums


    //types


    //attributes
    static protected Regex s_nameRegEx;

    //events
    public event RequestErrorHandler? RequestError;
    public event DataDownloadCompleteHandler? DataDownloadComplete;
    public event RealTimeDataUpdateHandler? RealTimeDataUpdate;

    //properties
    public virtual IList<string> Tickers { get => throw new NotImplementedException(); }
    public virtual int ConnectionCountMax { get => Environment.ProcessorCount; }

    //constructors
    static DataProviderPlugin()
    {
      //DataProvider name must be database safe since it's used in naming database tables.
      s_nameRegEx = new Regex(@"^[a-zA-Z][a-zA-Z0-9_\s,]*$");
    }

    public DataProviderPlugin(string name, string description) : base(name, description)
    {
      if (!s_nameRegEx.IsMatch(name)) throw new ArgumentException(string.Format("DataProvider name \"{0}\" is invalid, must be only alphanumeric characters and start with alphabetical character.", name));
    }

    //finalizers


    //interface implementations
    public abstract bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end);

    //methods
    protected virtual void raiseRequestError(string message, Exception? exception = null) { RequestError?.Invoke(this, new RequestErrorArgs(message, exception)); }
    protected virtual void raiseDataDownloadComplete(Instrument instrument, Resolution resolution, long count) { DataDownloadComplete?.Invoke(this, new DataDownloadCompleteArgs(instrument, resolution, count)); }
    protected virtual void raiseRealTimeDataUpdate(Instrument instrument, Resolution resolution, IList<BarData> barData, IList<Level1Data> level1Data) { RealTimeDataUpdate?.Invoke(this, new RealTimeDataUpdateArgs(instrument, resolution, barData, level1Data)); }
  }
}
