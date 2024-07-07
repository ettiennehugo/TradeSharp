using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for level1 price data.
  /// </summary>
  public interface ILevel1Data
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    DateTime DateTime { get; set; }
    double Bid { get; set; }
    double BidSize { get; set; }
    double Ask { get; set; }
    double AskSize { get; set; }
    double Last { get; set; }
    double LastSize { get; set; }
    string FormatMask { get; set; }
    string FormattedBid { get; }
    string FormattedAsk { get; }
    string FormattedLast { get; }

    //methods



  }
}
