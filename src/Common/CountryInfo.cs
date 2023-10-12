using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.VisualBasic;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using static System.Net.WebRequestMethods;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TradeSharp.Common
{
  /// <summary>
  /// Country information for available set of countries.
  /// </summary>
  public class CountryInfo : ObservableObject
  {
    //constants
    /// <summary>
    /// Set of defined countries with three letter iso-codes and English names.
    /// </summary>
    private static readonly Dictionary<string, string> s_englishNameByIso2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
        //{"ABW", "Aruba"},
        //{"AFG", "Afghanistan"},
        //{"AGO", "Angola"},
        //{"AIA", "Anguilla"},
        //{"ALA", "Åland Islands"},
        //{"ALB", "Albania"},
        //{"AND", "Andorra"},
        //{"ARE", "United Arab Emirates"},
        //{"ARG", "Argentina"},
        //{"ARM", "Armenia"},
        //{"ASM", "American Samoa"},
        //{"ATA", "Antarctica"},
        //{"ATF", "French Southern Territories"},
        //{"ATG", "Antigua and Barbuda"},
        {"AUS", "Australia"},
        //{"AUT", "Austria"},
        //{"AZE", "Azerbaijan"},
        //{"BDI", "Burundi"},
        {"BEL", "Belgium"},
        //{"BEN", "Benin"},
        //{"BES", "Bonaire, Sint Eustatius and Saba"},
        //{"BFA", "Burkina Faso"},
        //{"BGD", "Bangladesh"},
        //{"BGR", "Bulgaria"},
        //{"BHR", "Bahrain"},
        //{"BHS", "Bahamas"},
        //{"BIH", "Bosnia and Herzegovina"},
        //{"BLM", "Saint Barthélemy"},
        //{"BLR", "Belarus"},
        //{"BLZ", "Belize"},
        //{"BMU", "Bermuda"},
        //{"BOL", "Bolivia (Plurinational State of)"},
        //{"BRA", "Brazil"},
        //{"BRB", "Barbados"},
        //{"BRN", "Brunei Darussalam"},
        //{"BTN", "Bhutan"},
        //{"BVT", "Bouvet Island"},
        {"BWA", "Botswana"},
        //{"CAF", "Central African Republic"},
        {"CAN", "Canada"},
        //{"CCK", "Cocos (Keeling) Islands"},
        {"CHE", "Switzerland"},
        //{"CHL", "Chile"},
        {"CHN", "China"},
        //{"CIV", "Côte d'Ivoire"},
        //{"CMR", "Cameroon"},
        //{"COD", "Congo, Democratic Republic of the"},
        //{"COG", "Congo"},
        //{"COK", "Cook Islands"},
        //{"COL", "Colombia"},
        //{"COM", "Comoros"},
        //{"CPV", "Cabo Verde"},
        //{"CRI", "Costa Rica"},
        //{"CUB", "Cuba"},
        //{"CUW", "Curaçao"},
        //{"CXR", "Christmas Island"},
        {"CYM", "Cayman Islands"},
        {"CYP", "Cyprus"},
        //{"CZE", "Czechia"},
        {"DEU", "Germany"},
        //{"DJI", "Djibouti"},
        //{"DMA", "Dominica"},
        {"DNK", "Denmark"},
        //{"DOM", "Dominican Republic"},
        //{"DZA", "Algeria"},
        //{"ECU", "Ecuador"},
        //{"EGY", "Egypt"},
        //{"ERI", "Eritrea"},
        //{"ESH", "Western Sahara"},
        {"ESP", "Spain"},
        //{"EST", "Estonia"},
        //{"ETH", "Ethiopia"},
        {"FIN", "Finland"},
        //{"FJI", "Fiji"},
        //{"FLK", "Falkland Islands (Malvinas)"},
        {"FRA", "France"},
        //{"FRO", "Faroe Islands"},
        //{"FSM", "Micronesia (Federated States of)"},
        //{"GAB", "Gabon"},
        {"GBR", "United Kingdom of Great Britain and Northern Ireland"},
        //{"GEO", "Georgia"},
        //{"GGY", "Guernsey"},
        //{"GHA", "Ghana"},
        //{"GIB", "Gibraltar"},
        //{"GIN", "Guinea"},
        //{"GLP", "Guadeloupe"},
        //{"GMB", "Gambia"},
        //{"GNB", "Guinea-Bissau"},
        //{"GNQ", "Equatorial Guinea"},
        {"GRC", "Greece"},
        //{"GRD", "Grenada"},
        //{"GRL", "Greenland"},
        //{"GTM", "Guatemala"},
        //{"GUF", "French Guiana"},
        //{"GUM", "Guam"},
        //{"GUY", "Guyana"},
        {"HKG", "Hong Kong"},
        //{"HMD", "Heard Island and McDonald Islands"},
        //{"HND", "Honduras"},
        //{"HRV", "Croatia"},
        //{"HTI", "Haiti"},
        {"HUN", "Hungary"},
        //{"IDN", "Indonesia"},
        //{"IMN", "Isle of Man"},
        {"IND", "India"},
        //{"IOT", "British Indian Ocean Territory"},
        {"IRL", "Ireland"},
        {"IRN", "Iran"},
        //{"IRQ", "Iraq"},
        {"ISL", "Iceland"},
        {"ISR", "Israel"},
        {"ITA", "Italy"},
        //{"JAM", "Jamaica"},
        //{"JEY", "Jersey"},
        {"JOR", "Jordan"},
        {"JPN", "Japan"},
        //{"KAZ", "Kazakhstan"},
        //{"KEN", "Kenya"},
        //{"KGZ", "Kyrgyzstan"},
        //{"KHM", "Cambodia"},
        //{"KIR", "Kiribati"},
        //{"KNA", "Saint Kitts and Nevis"},
        //{"KOR", "Korea, Republic of"},
        //{"KWT", "Kuwait"},
        //{"LAO", "Lao People's Democratic Republic"},
        //{"LBN", "Lebanon"},
        //{"LBR", "Liberia"},
        //{"LBY", "Libya"},
        //{"LCA", "Saint Lucia"},
        //{"LIE", "Liechtenstein"},
        //{"LKA", "Sri Lanka"},
        //{"LSO", "Lesotho"},
        //{"LTU", "Lithuania"},
        //{"LUX", "Luxembourg"},
        //{"LVA", "Latvia"},
        //{"MAC", "Macao"},
        //{"MAF", "Saint Martin (French part)"},
        //{"MAR", "Morocco"},
        //{"MCO", "Monaco"},
        //{"MDA", "Moldova, Republic of"},
        //{"MDG", "Madagascar"},
        //{"MDV", "Maldives"},
        {"MEX", "Mexico"},
        //{"MHL", "Marshall Islands"},
        //{"MKD", "North Macedonia"},
        //{"MLI", "Mali"},
        //{"MLT", "Malta"},
        //{"MMR", "Myanmar"},
        //{"MNE", "Montenegro"},
        //{"MNG", "Mongolia"},
        //{"MNP", "Northern Mariana Islands"},
        //{"MOZ", "Mozambique"},
        //{"MRT", "Mauritania"},
        //{"MSR", "Montserrat"},
        //{"MTQ", "Martinique"},
        //{"MUS", "Mauritius"},
        //{"MWI", "Malawi"},
        //{"MYS", "Malaysia"},
        //{"MYT", "Mayotte"},
        //{"NAM", "Namibia"},
        //{"NCL", "New Caledonia"},
        //{"NER", "Niger"},
        //{"NFK", "Norfolk Island"},
        //{"NGA", "Nigeria"},
        //{"NIC", "Nicaragua"},
        //{"NIU", "Niue"},
        {"NLD", "Netherlands"},
        //{"NOR", "Norway"},
        //{"NPL", "Nepal"},
        //{"NRU", "Nauru"},
        {"NZL", "New Zealand"},
        //{"OMN", "Oman"},
        {"PAK", "Pakistan"},
        //{"PAN", "Panama"},
        //{"PCN", "Pitcairn"},
        //{"PER", "Peru"},
        //{"PHL", "Philippines"},
        //{"PLW", "Palau"},
        //{"PNG", "Papua New Guinea"},
        {"POL", "Poland"},
        //{"PRI", "Puerto Rico"},
        {"PRK", "South Korea"},
        {"PRT", "Portugal"},
        //{"PRY", "Paraguay"},
        //{"PSE", "Palestine, State of"},
        //{"PYF", "French Polynesia"},
        //{"QAT", "Qatar"},
        //{"REU", "Réunion"},
        //{"ROU", "Romania"},
        //{"RUS", "Russian Federation"},
        //{"RWA", "Rwanda"},
        {"SAU", "Saudi Arabia"},
        //{"SDN", "Sudan"},
        //{"SEN", "Senegal"},
        //{"SGP", "Singapore"},
        //{"SGS", "South Georgia and the South Sandwich Islands"},
        //{"SHN", "Saint Helena, Ascension and Tristan da Cunha"},
        //{"SJM", "Svalbard and Jan Mayen"},
        //{"SLB", "Solomon Islands"},
        //{"SLE", "Sierra Leone"},
        //{"SLV", "El Salvador"},
        //{"SMR", "San Marino"},
        //{"SOM", "Somalia"},
        //{"SPM", "Saint Pierre and Miquelon"},
        //{"SRB", "Serbia"},
        //{"SSD", "South Sudan"},
        //{"STP", "Sao Tome and Principe"},
        //{"SUR", "Suriname"},
        //{"SVK", "Slovakia"},
        //{"SVN", "Slovenia"},
        {"SWE", "Sweden"},
        //{"SWZ", "Eswatini"},
        //{"SXM", "Sint Maarten (Dutch part)"},
        //{"SYC", "Seychelles"},
        //{"SYR", "Syrian Arab Republic"},
        //{"TCA", "Turks and Caicos Islands"},
        //{"TCD", "Chad"},
        //{"TGO", "Togo"},
        //{"THA", "Thailand"},
        //{"TJK", "Tajikistan"},
        //{"TKL", "Tokelau"},
        //{"TKM", "Turkmenistan"},
        //{"TLS", "Timor-Leste"},
        //{"TON", "Tonga"},
        //{"TTO", "Trinidad and Tobago"},
        //{"TUN", "Tunisia"},
        //{"TUR", "Türkiye"},
        //{"TUV", "Tuvalu"},
        {"TWN", "Taiwan"},
        //{"TZA", "Tanzania, United Republic of"},
        //{"UGA", "Uganda"},
        //{"UKR", "Ukraine"},
        //{"UMI", "United States Minor Outlying Islands"},
        //{"URY", "Uruguay"},
        {"USA", "United States of America"},
        //{"UZB", "Uzbekistan"},
        //{"VAT", "Holy See"},
        //{"VCT", "Saint Vincent and the Grenadines"},
        //{"VEN", "Venezuela (Bolivarian Republic of)"},
        //{"VGB", "Virgin Islands (British)"},
        //{"VIR", "Virgin Islands (U.S.)"},
        //{"VNM", "Viet Nam"},
        //{"VUT", "Vanuatu"},
        //{"WLF", "Wallis and Futuna"},
        //{"WSM", "Samoa"},
        //{"YEM", "Yemen"},
        {"ZAF", "South Africa"}
        //{"ZMB", "Zambia"},
        //{"ZWE", "Zimbabwe"}
      };


    //enums


    //types


    //attributes
    protected string m_imagePath;
    protected CultureInfo m_cultureInfo;
    protected RegionInfo m_regionInfo;

    //constructors
    public CountryInfo(CultureInfo cultureInfo, RegionInfo regionInfo)
    {
      m_cultureInfo = cultureInfo;
      m_regionInfo = regionInfo;
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");
      m_imagePath = $"{tradeSharpHome}\\data\\assets\\countryflags\\w80\\{regionInfo.TwoLetterISORegionName}.png";
    }

    //finalizers


    //interface implementations


    //properties
    public static IDictionary<string, string> EnglishNameByIso2 { get => s_englishNameByIso2; }
    public string ImagePath { get => m_imagePath; set => SetProperty(ref m_imagePath, value); }
    public CultureInfo CultureInfo { get => m_cultureInfo; set => SetProperty(ref m_cultureInfo, value); }
    public RegionInfo RegionInfo { get => m_regionInfo; set => SetProperty(ref m_regionInfo, value); }

    //methods
    public static CountryInfo? GetCountryInfo(string isoCode)
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);
        
        foreach (CultureInfo cultureInfo in cultures)
        {
          RegionInfo regionInfo = new RegionInfo(cultureInfo.LCID);
          if (regionInfo.ThreeLetterISORegionName == isoCode)
            return new CountryInfo(cultureInfo, regionInfo);
        }

      return null;
    }
  }

}
