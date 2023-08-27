namespace TradeSharp.Data
{
  /// <summary>
  /// Encapsualtes the supported categories of fundamentals that can be associated with a specific tradeable instrument. E.g. fundamentals for
  /// Forex and futures would be based on country specific economic indicators/fundamental factors like GDP while a stock would be based on
  /// company specific fundamental factors like revenue.
  /// </summary>
  public enum FundamentalCategory
  {
    None,
    Country,
    Instrument,
  }

  /// <summary>
  /// Encapsulates the release interval for fundamental data.
  /// </summary>
  public enum FundamentalReleaseInterval
  {
    Unknown,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
  }

  /// <summary>
  /// Interface for fundamental factor definitions associated with a countries or instruments. The sub-interfaces will introduce the country and instrument
  /// notions further with the retrieval of actual values associated with the fundaments.
  /// </summary>
  public interface IFundamental : IName, IDescription
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Unique id for the object.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Category of instruments that the fundamental factor can be associated with.
    /// </summary>
    FundamentalCategory Category { get; }

    /// <summary>
    /// Interval at which the fundamental factor is reported.
    /// </summary>
    FundamentalReleaseInterval ReleaseInterval { get; }


    //methods


  }
}