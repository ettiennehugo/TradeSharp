namespace TradeSharp.Data
{
  /// <summary>
  /// Fundamental factor associated with a specific country.
  /// </summary>
  public interface ICountryFundamental : IFundamentalValues
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Returns the country id for this association.
    /// </summary>
    Guid CountryId { get; }

    /// <summary>
    /// Country to which the fundamental factor is bound.
    /// </summary>
    ICountry Country { get; internal set; }

    //methods


  }
}