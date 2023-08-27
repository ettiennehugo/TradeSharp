using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Different types of resolutions that can be supported by a data provider/data feed. 
  /// </summary>
  public enum Resolution 
  {
    Minute,
    Hour,
    Day,
    Week,
    Month,
    Level1,   //tick data
    //Level2,   //order book data - currently not supported
  }


  /// <summary>
  /// Model change structure used to communicate changes in the general data model to observers of data manager.
  /// </summary>
  public enum ModelChangeType
  {
    Create,
    Update,
    Delete
  }
 
  public struct ModelChange
  {
    public ModelChangeType ChangeType { get; set; }
    public object Object { get; set; }
  }

  /// <summary>
  /// Fundamental data change structure used to communicate changes in fundamental data to observers of data manager.
  /// </summary>
  /// 
  public enum FundamentalChangeType
  {
    Create,
    Update,
    Delete
  }

  public struct FundamentalChange
  {
    public FundamentalChangeType ChangeType { get; set; }
    public IFundamentalValues FundamentalValues { get; set; }
    public DateTime DateTime { get; set; }
    public double Value { get; set; }
  }

  /// <summary>
  /// Price data change structure used to communicate changes in price data to observers of data manager.
  /// </summary>
  public enum PriceChangeType
  {
    Update,
    Delete,
  }

  public struct PriceChange
  {
    public PriceChangeType ChangeType { get; set; }
    public IInstrument Instrument { get; set; }
    public Resolution Resolution { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
  }
}
