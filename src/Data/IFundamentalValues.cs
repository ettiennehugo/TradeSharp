using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base interface tying a fundamental factor to it's associated values over time.
  /// </summary>
  public interface IFundamentalValues
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Returns the fundamental factor id for this association.
    /// </summary>
    Guid FundamentalId { get; }

    /// <summary>
    /// Fundamental factor definition associated with instrument.
    /// </summary>
    IFundamental Fundamental { get; internal set; }

    /// <summary>
    /// Latest reported value for the fundamental factor.
    /// </summary>
    KeyValuePair<DateTime, decimal>? Latest { get; }

    /// <summary>
    /// List of historic values and associated dates for the fundamental factor.
    /// </summary>
    IDictionary<DateTime, decimal> Values { get; }


    //methods



  }
}
