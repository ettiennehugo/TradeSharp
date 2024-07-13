using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.Common
{
  /// <summary>
  /// Country code information.
  /// </summary>
  public struct CountryCode
  {
    public string IsoCode { get; }
    public string TwoLetterCode { get; }
    public string ThreeLetterCode { get; }
    public string EnglishName { get; }
    public string CurrencyCode { get; }

    public CountryCode(string isoCode, string twoLetterCode, string threeLetterCode, string englishName, string currencyCode)
    {
      IsoCode = isoCode;
      TwoLetterCode = twoLetterCode;
      ThreeLetterCode = threeLetterCode;
      EnglishName = englishName;
      CurrencyCode = currencyCode;
    }
  }

  /// <summary>
  /// Country information for available set of countries.
  /// </summary>
  public class CountryInfo : ObservableObject
  {
    //constants
    /// <summary>
    /// Default Id used for international objects - will default to the local system settings.
    /// </summary>
    public const string InternationalId = "999";

    /// <summary>
    /// Set of defined countries with three letter iso-codes, English names, and currencies.
    /// </summary>
    private static readonly List<CountryCode> s_countryCodes = new List<CountryCode>()
    {
      new CountryCode("en-AU", "AU", "AUS", "Australia", "AUD"),
      new CountryCode("nl-BE", "BE", "BEL", "Belgium", "EUR"),
      new CountryCode("en-CA", "CA", "CAN", "Canada", "CAD"),
      new CountryCode("en-KY", "KY", "CYM", "Cayman Islands", "KYD"),
      new CountryCode("fi-FI", "FI", "FIN", "Finland", "EUR"),
      new CountryCode("fr-FR", "FR", "FRA", "France", "EUR"),
      new CountryCode("de-DE", "DE", "DEU", "Germany", "EUR"),
      new CountryCode("gr-GR", "GR", "GRC", "Greece", "EUR"),
      new CountryCode("it-VA", "VA", "VAT", "Holy See (the)", "EUR"),
      new CountryCode("is-IS", "IS", "ISL", "Iceland", "ISK"),
      new CountryCode("hi-IN", "IN", "IND", "India", "INR"),
      new CountryCode("ar-IR", "IR", "IRN", "Iran (Islamic Republic of)", "IRR"),
      new CountryCode("en-IE", "IE", "IRL", "Ireland", "EUR"),
      new CountryCode("en-IM", "IM", "IMN", "Isle of Man", "GBP"),
      new CountryCode("he-IL", "IL", "ISR", "Israel", "ILS"),
      new CountryCode("it-IT", "IT", "ITA", "Italy", "EUR"),
      new CountryCode("jp-JP", "JP", "JPN", "Japan", "JPY"),
      new CountryCode("ar-JO", "JO", "JOR", "Jordan", "JOD"),
      new CountryCode("de-LU", "LU", "LUX", "Luxembourg", "EUR"),
      new CountryCode("es-MX", "MX", "MEX", "Mexico", "MXN"),
      new CountryCode("nl-NL", "NL", "NLD", "Netherlands (the)", "EUR"),
      new CountryCode("en-NZ", "NZ", "NZL", "New Zealand", "NZD"),
      new CountryCode("nn-NO", "NO", "NOR", "Norway", "NOK"),
      new CountryCode("ur-PK", "PK", "PAK", "Pakistan", "PKR"),
      new CountryCode("pl-PL", "PL", "POL", "Poland", "PLN"),
      new CountryCode("pt-PT", "PT", "PRT", "Portugal", "EUR"),
      new CountryCode("ar-QA", "QA", "QAT", "Qatar", "QAR"),
      new CountryCode("ru-RU", "RU", "RUS", "Russian Federation", "RUB"),
      new CountryCode("en-SG", "SG", "SGP", "Singapore", "SGD"),
      new CountryCode("en-ZA", "ZA", "ZAF", "South Africa", "ZAR"),
      new CountryCode("se-SE", "SE", "SWE", "Sweden", "SEK"),
      new CountryCode("de-CH", "CH", "CHE", "Switzerland", "CHF"),
      new CountryCode("th-TH", "TH", "THA", "Thailand", "THB"),
      new CountryCode("ar-TR", "TR", "TUR", "Turkey", "TRY"),
      new CountryCode("ar-AE", "AE", "ARE", "United Arab Emirates", "AED"),
      new CountryCode("en-GB", "GB", "GBR", "United Kingdom of Great Britain and Northern Ireland", "GBP"),
      new CountryCode("en-US", "US", "USA", "United States of America", "USD"),
    };

    //enums


    //types


    //attributes
    protected string m_isoCode;
    protected string m_imagePath;
    protected CultureInfo m_cultureInfo;
    protected RegionInfo m_regionInfo;

    //constructors
    //TODO: See whether you can create a custom culture in .Net8 here.
    //  - https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureandregioninfobuilder?view=netframework-4.8.1&devlangs=csharp&f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(CultureAndRegionInfoBuilder)%3Bk(SolutionItemsProject)%3Bk(DevLang-csharp)%26rd%3Dtrue
    //static CountryInfo()
    //{
    //  CultureAndRegionInfoBuilder internationalCulture = new CultureAndRegionInfoBuilder(InternationalId, CultureAndRegionModifiers.None);
    //  internationalCulture.LoadDataFromCultureInfo(CultureInfo.CurrentCulture);
    //  internationalCulture.LoadDataFromRegionInfo(RegionInfo.CurrentRegion);
    //  internationalCulture.CultureEnglishName = "International Culture";
    //  internationalCulture.CultureNativeName = "International Culture";
    //  internationalCulture.CurrencyNativeName = RegionInfo.CurrentRegion.CurrencyNativeName;
    //  internationalCulture.Register();
    //}

    //finalizers


    //interface implementations


    //properties
    public static IList<CountryCode> CountryCodes { get => s_countryCodes; }
    public string ImagePath { get => m_imagePath; set => SetProperty(ref m_imagePath, value); }
    public CultureInfo CultureInfo { get => m_cultureInfo; set => SetProperty(ref m_cultureInfo, value); }
    public RegionInfo RegionInfo { get => m_regionInfo; set => SetProperty(ref m_regionInfo, value); }

    //methods


    //types


    //constructors
    public CountryInfo(string isoCode, CultureInfo cultureInfo, RegionInfo regionInfo)
    {
      m_isoCode = isoCode;
      m_cultureInfo = cultureInfo;
      m_regionInfo = regionInfo;
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");

      if (m_isoCode != InternationalId)
        m_imagePath = $"{tradeSharpHome}\\data\\assets\\countryflags\\w80\\{regionInfo.TwoLetterISORegionName}.png";
      else
        m_imagePath = $"{tradeSharpHome}\\data\\assets\\countryflags\\w80\\{InternationalId}.png";    //HACK: Since we can not create a custom culture we settle for a different icon and the user's local settings (see below).
    }

    //finalizers


    //interface implementations


    //methods
    public static CountryInfo? GetCountryInfo(string isoCode)
    {
      if (isoCode == InternationalId)
      {
        //return special international culture and region
        return new CountryInfo(isoCode, CultureInfo.CurrentCulture, RegionInfo.CurrentRegion);
      }
      else
      {
        RegionInfo regionInfo = new RegionInfo(isoCode);
        CultureInfo cultureInfo = new CultureInfo(isoCode);
        return new CountryInfo(isoCode, cultureInfo, regionInfo);
      }
    }
  }
}

