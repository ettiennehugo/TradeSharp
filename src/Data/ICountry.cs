namespace TradeSharp.Data
{
  /// <summary>
  /// Country definition.
  /// </summary>
  public interface ICountry : IName
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid Id { get; }
    string Currency { get; }
    string CurrencySymbol { get; }
    string IsoCode { get; }
    string LanguageCode { get; }
    string RegionCode { get; }
    IList<IExchange> Exchanges { get; }
    IList<ICountryFundamental> Fundamentals { get; }
    IList<IHoliday> Holidays { get; }

    //methods


  }
}