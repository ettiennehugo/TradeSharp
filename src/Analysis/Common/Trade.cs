using TradeSharp.Data;

namespace TradeSharp.Analysis.Common
{

  //TODO:
  // - Add methods to add metrics to a trade.
  // - Constructor takes the entry datetime.
  // - TBD: How will the exit date be set/determined.
  // - Entry/exit commission

  /// <summary>
  /// Implementation of class representing a trade made through the system.
  /// </summary>
  public class Trade : Computation
    {
        //constants


        //enums


        //types


        //attributes


        //constructors


        //finalizers


        //interface implementations


        //properties
        Instrument Instrument => throw new NotImplementedException();

        //TBD: How will the trade return it's set of metrics? Currently there is MetricSingleResult and MetricSeriesResult but there is NO MetricResult base class.
        //IReadOnlyList<MetricResult> Metrics => throw new NotImplementedException();

        DateTime Entry => throw new NotImplementedException();

        DateTime Exit => throw new NotImplementedException();

        //methods



    }
}
