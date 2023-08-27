namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for sessions defined on an exchange.
  /// </summary>
  public interface ISession : IName
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid Id { get; }
    IExchange Exchange { get; internal set; }
    DayOfWeek Day { get; }
    TimeOnly End { get; }
    TimeOnly Start { get; }

    //methods



  }
}