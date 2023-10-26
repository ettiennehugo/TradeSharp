using System.Collections.Generic;
using System.Drawing;
using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualBasic;
using static System.Net.WebRequestMethods;

namespace TradeSharp.Common
{
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
    /// Set of defined countries with three letter iso-codes and English names.
    /// </summary>
    private static readonly List<(string, string, string, string)> s_countryCodes = new List<(string, string, string, string)>()
      {
        ("en-AU", "AU", "AUS", "Australia"),
        ("nl-BE", "BE", "BEL", "Belgium"),
        ("en-CA", "CA", "CAN", "Canada"),
        ("en-KY", "KY", "CYM", "Cayman Islands (the)"),
        ("fi-FI", "FI", "FIN", "Finland"),
        ("fr-FR", "FR", "FRA", "France"),
        ("de-DE", "DE", "DEU", "Germany"),
        ("gr-GR", "GR", "GRC", "Greece"),
        ("it-VA", "VA", "VAT", "Holy See (the)"),
        ("is-IS", "IS", "ISL", "Iceland"),
        ("hi-IN", "IN", "IND", "India"),
        ("ar-IR", "IR", "IRN", "Iran (Islamic Republic of)"),
        ("en-IE", "IE", "IRL", "Ireland"),
        ("en-IM", "IM", "IMN", "Isle of Man"),
        ("he-IL", "IL", "ISR", "Israel"),
        ("it-IT", "IT", "ITA", "Italy"),
        ("jp-JP", "JP", "JPN", "Japan"),
        ("ar-JO", "JO", "JOR", "Jordan"),
        ("de-LU", "LU", "LUX", "Luxembourg"),
        ("es-MX", "MX", "MEX", "Mexico"),
        ("nl-NL", "NL", "NLD", "Netherlands (the)"),
        ("en-NZ", "NZ", "NZL", "New Zealand"),
        ("nn-NO", "NO", "NOR", "Norway"),
        ("ur-PK", "PK", "PAK", "Pakistan"),
        ("pl-PL", "PL", "POL", "Poland"),
        ("pt-PT", "PT", "PRT", "Portugal"),
        ("ar-QA", "QA", "QAT", "Qatar"),
        ("ru-RU", "RU", "RUS", "Russian Federation (the)"),
        ("en-SG", "SG", "SGP", "Singapore"),
        ("en-ZA", "ZA", "ZAF", "South Africa"),
        ("se-SE", "SE", "SWE", "Sweden"),
        ("de-CH", "CH", "CHE", "Switzerland"),
        ("th-TH", "TH", "THA", "Thailand"),
        ("ar-TR", "TR", "TUR", "Turkey"),
        ("ar-AE", "AE", "ARE", "United Arab Emirates (the)"),
        ("en-GB", "GB", "GBR", "United Kingdom of Great Britain and Northern Ireland (the)"),
      };

    //enums


    //types


    //attributes
    protected string m_isoCode;
    protected string m_imagePath;
    protected CultureInfo m_cultureInfo;
    protected RegionInfo m_regionInfo;

    //constructors
    // TODO: In .Net 8 there is a custom CountryAndRegionInfoBuilder that allows custom defintions, rather use that for international culture and reqion definitions.
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


    //properties
    public static IList<(string, string, string, string)> CountryCodes { get => s_countryCodes; }
    public string ImagePath { get => m_imagePath; set => SetProperty(ref m_imagePath, value); }
    public CultureInfo CultureInfo { get => m_cultureInfo; set => SetProperty(ref m_cultureInfo, value); }
    public RegionInfo RegionInfo { get => m_regionInfo; set => SetProperty(ref m_regionInfo, value); }

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
