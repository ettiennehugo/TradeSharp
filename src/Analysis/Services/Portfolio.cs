using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Analysis;

namespace TradeSharp.Analysis
{
  
  //TODO
  // - Bring together a set of instruments to trade
  //   - Instruments should be traded over different resolutions
  //   - Should allow scanners to be added to select instruments to trade
  //   - Allow scanning on one resolution to identify instruments (e.g. days, weeks) to trade and then trade on another resolution (e.g. minutes, hours)
  // - Evaluate the data used to determine how complete the data is to make trading decisions
  // - Allow trading instruments over a start/end date and trading at different resolutions.
  // - The portfolio can be passed into a backtester to simulate trading.
  // - How will you simulate trading costs? (Spread, commission, slippage)
  // - How will you simulate trading constraints? (Leverage, margin, position sizing)
  //   - Allow adding multiple rules in an ordered list and the first rule that applies is used.

  /// <summary>
  /// Represents a specialization of the engine configuration that allows evaluation of a portfolio of instruments being traded.
  /// </summary>
  public class Portfolio : EngineConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Portfolio() { }

    //finalizers


    //interface implementations


    //properties
    public IReadOnlyList<Metric> Metrics => throw new NotImplementedException();

    //methods


  }
}
