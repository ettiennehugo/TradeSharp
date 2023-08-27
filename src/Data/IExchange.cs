namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for exchanges defined in different countries and timezones.
  /// </summary>
  public interface IExchange : IName
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    Guid Id { get; }
    ICountry Country { get; internal set; }
    IList<IExchangeHoliday> Holidays { get; }
    IDictionary<string, IInstrument> Instruments { get; }
    IDictionary<DayOfWeek, IList<ISession>> Sessions { get; }
    TimeZoneInfo TimeZone { get; }

    //methods

  }
}