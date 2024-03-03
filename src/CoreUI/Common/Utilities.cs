using Microsoft.Extensions.Logging;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// General utilities used throughout the application. 
  /// NOTE: This class integrates with some of the repositories/services to perform more advanced operations. For simpler operations, use the Utilities class in the Common or Data project.
  /// </summary>
  public class Utilities
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
    public static IList<string> secondaryExchangeNames(Instrument instrument, IList<Exchange> exchanges, ILogger? logger = null)
    {
      var result = new List<string>();
      foreach (var secondaryId in instrument.SecondaryExchangeIds)
      {
        var exchange = exchanges.FirstOrDefault(e => e.Id == secondaryId);
        if (exchange != null)
          result.Add(exchange.Name);
        else
          logger?.LogWarning($"Secondary exchange with id {secondaryId} not found for instrument {instrument.Ticker}.");
      }
      return result;
    }

    public static string secondaryExchangeNamesCsv(Instrument instrument, IList<Exchange> exchanges, ILogger? logger = null)
    {
      var list = secondaryExchangeNames(instrument, exchanges, logger);
      return TradeSharp.Common.Utilities.MakeCsvSafe(string.Join(",", list));
    }
  }
}
