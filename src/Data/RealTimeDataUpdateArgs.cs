namespace TradeSharp.Data
{
  public class RealTimeDataUpdateArgs
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public Instrument Instrument { get; protected set; }
    public Resolution Resolution { get; protected set; }
    public IList<BarData> Bars { get; protected set; }
    public IList<Level1Data> Level1 { get; protected set; }

    //constructors
    public RealTimeDataUpdateArgs(Instrument instrument, Resolution resolution, IList<BarData> bars, IList<Level1Data> level1)
    {
      Instrument = instrument;
      Resolution = resolution;
      Bars = bars;
      Level1 = level1;
    }

    //finalizers


    //interface implementations


    //methods




  }
}
