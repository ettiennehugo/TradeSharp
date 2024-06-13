using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Direction of the position held.
  /// </summary>
  public enum PositionDirection
  {
    Flat,
    Long,
    Short
  }

  /// <summary>
  /// Trading position held within a specific account at a broker.
  /// </summary>
  [ComVisible(true)]
  [Guid("B2054674-8C40-4BAA-8BE8-D1D6CAFDC18B")]
  public partial class Position: ObservableObject
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Position(Account account, Instrument instrument, PositionDirection direction, double size, decimal averageCost, decimal marketValue, decimal marketPrice, decimal unrealizedPnl, decimal realizedPnl)
    {
      Account = account;
      Instrument = instrument;
      Direction = direction;
      Size = size;
      AverageCost = averageCost;
      MarketValue = marketValue;
      MarketPrice = marketPrice;
      UnrealizedPnl = unrealizedPnl;
      RealizedPnl = realizedPnl;
      CustomProperties = new Dictionary<string, CustomProperty>();
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private Account m_account;
    [ObservableProperty] private Instrument m_instrument;
    [ObservableProperty] private PositionDirection m_direction;
    [ObservableProperty] private double m_size;
    [ObservableProperty] private decimal m_averageCost;
    [ObservableProperty] private decimal m_marketValue;
    [ObservableProperty] private decimal m_marketPrice;
    [ObservableProperty] private decimal m_unrealizedPnl;
    [ObservableProperty] private decimal m_realizedPnl;
    public IDictionary<string, CustomProperty> CustomProperties { get; internal set; }

    //methods


  }
}
