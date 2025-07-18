﻿namespace TradeSharp.Data
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
    public IList<IBarData>? Bars { get; protected set; }        //can return multiple bar updates when the bar resolution is very small, e.g. 1-second
    public IList<ILevel1Data>? Level1 { get; protected set; }

    //constructors
    public RealTimeDataUpdateArgs(Instrument instrument, Resolution resolution, IList<IBarData>? bars, IList<ILevel1Data>? level1)
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
