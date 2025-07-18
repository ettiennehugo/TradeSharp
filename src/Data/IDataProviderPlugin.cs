﻿using System.Runtime.InteropServices;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  //Exposing components to COM
  //https://learn.microsoft.com/en-us/dotnet/framework/interop/exposing-dotnet-components-to-com
  //https://learn.microsoft.com/en-us/dotnet/core/native-interop/expose-components-to-com
  //https://learn.microsoft.com/en-us/dotnet/standard/native-interop/

  /// <summary>
  /// Interface to be supported by data provider plugins.
  /// </summary>
  [ComVisible(true)]
  [Guid("A7396674-D60B-489C-83C2-39BD6466C0FC")]
  public interface IDataProviderPlugin: IPlugin
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Set of tickers supported by the data provider.
    /// </summary>
    IList<string> Tickers { get; }
    int ConnectionCountMax { get; }

    //events
    event RequestErrorHandler? RequestError;    //event raised when a request error occurs
    event DataDownloadCompleteHandler? DataDownloadComplete;    //event raised when historical data download is complete for an instrument and resolution
    event RealTimeDataUpdateHandler? RealTimeDataUpdate;    //event raised when real-time data is updated for an instrument and resolution

    //methods
    /// <summary>
    /// Request historical data for a specific instrument with a given resolution and time range.
    /// </summary>
    bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end);

    /// <summary>
    /// Subscribe to real-time data for a specific instrument with a given resolution.
    /// </summary>
    bool Subscribe(Instrument instrument, Resolution resolution);

    /// <summary>
    /// Unsubscribe from real-time data for a specific instrument with a given resolution.
    /// </summary>
    bool Unsubscribe(Instrument instrument, Resolution resolution);
  }
}
