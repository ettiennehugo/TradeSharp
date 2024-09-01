namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Single metric value computed over a timeseries of data.
  /// </summary>
  public class SingleMetricResult : Function
    {
        //constants


        //enums


        //types


        //attributes


        //constructors
        public SingleMetricResult(Metric metric)
        {
            Metric = metric;
        }

        //finalizers


        //interface implementations


        //properties
        public Metric Metric { get; }
        public decimal Value => throw new NotImplementedException();

        //methods


    }
}
