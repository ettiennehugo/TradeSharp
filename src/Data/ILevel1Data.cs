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
    public DateTime DateTime { get; set; }
    public double Bid { get; set; }
    public double BidSize { get; set; }
    public double Ask { get; set; }
    public double AskSize { get; set; }
    public double Last { get; set; }
    public double LastSize { get; set; }

    //methods



  }
}
