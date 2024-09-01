namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Series of metric values calculated over a timeseries of data.
  /// </summary>
  public class SeriesMetricResult : SingleMetricResult
    {
        //constants


        //enums


        //types


        //attributes


        //constructors
        public SeriesMetricResult(Metric metric) : base(metric)
        {
            Values = new List<(DateTime, decimal)>();
        }

        //finalizers


        //interface implementations


        //properties
        public IReadOnlyList<(DateTime, decimal)> Values { get; protected set; }

        //methods


    }
}
