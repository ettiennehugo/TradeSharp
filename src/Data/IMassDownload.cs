using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Functional interface to to be supported by data provider plugins that can download large amounts
  /// of data at one time.
  /// </summary>
  [ComVisible(true)]
  [Guid("78B0087D-A191-4F3E-9799-D203470E8AC7")]
  public interface IMassDownload
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties


    //methods
    /// <summary>
    /// Downloads the available data from the DataProvider between the given start/end times [inclusive] at the given resolution.
    /// The plugin must download only the specified tickers if available and potentially generate a progress and log of what could
    /// be downloaded.
    /// </summary>
    void Download(DateTime start, DateTime end, Resolution resolution, IList<string> tickers);

  }
}
