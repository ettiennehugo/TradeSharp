using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Common;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// Type of a tradeable instrument.
  /// </summary>
  public enum InstrumentType
  {
    None = 0,       //only used for initialization
    Unknown,
    NotSupported,   //instrument type not supported
    Index,          //non-tradeable index
    [Description("Exchange Traded Fund")]
    ETF,
    Stock,
    Forex,
    Crypto,
    Future,
    Option,
    MutualFund,
    [Description("Contract for Difference")]
    CFD,
    Bond,
    Commodity,
  }

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
  /// Control attributes used for DataObject type classes.
  /// </summary>
  [Flags]
  public enum Attributes : long
  {
    None = 0,
    Editable = 1 << 0,
    Deletable = 1 << 1,
  }

  /// <summary>
  /// Base class for data store service objects. This class is used to define the basic properties and methods that all data store service objects
  /// </summary>
  public partial class DataObject : ObservableObject
  {
    public const Attributes DefaultAttributes = Attributes.Editable | Attributes.Deletable;

    public DataObject(Guid id, Attributes attributeSet, string tag)
    {
      Id = id;
      AttributeSet = attributeSet;
      m_tagStr = tag;
    }

    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private Attributes m_attributeSet;
    
    protected string m_tagStr;
    public string TagStr {
      get => m_tagStr;
      set {
        if (TagStr != value)
        {
          m_tagStr = value;
          m_tag = null; //clear the tag value since it has changed
          OnPropertyChanged("TagStr");
        }
      } 
    }

    public bool HasTagData { get => !string.IsNullOrEmpty(TagStr) && TagStr != TagValue.EmptyJson; }

    protected TagValue? m_tag = null;
    public TagValue Tag { 
      get {
        //lazy load the tag value since parsing the JSON could be expensive
        if (m_tag == null)
        {
          if (TagStr.Length > 0)
          {
            try
            {
              m_tag = TagValue.From(TagStr);
            }
            catch (Exception)
            {
              m_tag = new TagValue();
            }
          }
          else
            m_tag = new TagValue();
          m_tag.EntriesChanged += handleTagChange;
        }
        return m_tag!;
      } 
    }

    public bool HasAttribute(Attributes attribute)
    {
      return (AttributeSet & attribute) == attribute;
    }

    public void SetAttribute(Attributes attribute)
    {
      AttributeSet = AttributeSet | attribute;
    }

    public void ResetAttribute(Attributes attribute)
    {
      AttributeSet = AttributeSet & ~attribute; 
    }

    /// <summary>
    /// Toggle attribute and return it's new value.
    /// </summary>
    public bool ToggleAttribute(Attributes attribute)
    {
      if (HasAttribute(attribute))
        ResetAttribute(attribute);
      else
        SetAttribute(attribute);
      return HasAttribute(attribute);
    }

    /// <summary>
    /// Reflect changes to the Tag object into the TagStr JSON string. This is important to keep
    /// the database data stored from the string in sync with the object.
    /// </summary>
    protected void handleTagChange(object sender, TagEntry? entry)
    {      
      OnPropertyChanged("Tag");
      TagStr = Tag.ToJson();
    }
  }

  /// <summary>
  /// Storage class for country data.
  /// </summary>
  public partial class Country : DataObject, IEquatable<Country>
  {
    /// <summary>
    /// Special Id for international "country" used for objects that require international access.
    /// </summary>
    private static string s_internationalIdStr = "11111111-1111-1111-1111-111111111111";
    private static Guid s_internationalId = Guid.Parse(s_internationalIdStr);
    public static Guid InternationalId { get => s_internationalId; }
    private static string s_internationalIsoCode = CountryInfo.InternationalsoCode;
    public static string InternationalIsoCode = s_internationalIsoCode; //three letter iso codes use alphabetical characters so using numbers should be good

    /// <summary>
    /// Return the flag image path to use for the given country code.
    /// </summary>
    public static string GetFlagPath(string isoCode)
    {
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");

      //use fallback based on different image types, use png when possible since it supports proper transparency
      string logoFilename = $"{tradeSharpHome}\\data\\countryflags\\w80\\{isoCode}.png";
      if (File.Exists(logoFilename)) return logoFilename;
      logoFilename = $"{tradeSharpHome}\\data\\countryflags\\w80\\{isoCode}.jpg";
      if (File.Exists(logoFilename)) return logoFilename;
      logoFilename = $"{tradeSharpHome}\\data\\countryflags\\w80\\{isoCode}.jpeg";
      if (File.Exists(logoFilename)) return logoFilename;
      return $"{tradeSharpHome}\\data\\countryflags\\w80\\{s_internationalIsoCode}.png"; //return no logo image
    }

    public Country(Guid id, Attributes attributeSet, string tag, string isoCode) : base(id, attributeSet, tag)
    {
      IsoCode = isoCode;
      CountryInfo = CountryInfo.GetCountryInfo(IsoCode);
    }

    [ObservableProperty] private string m_isoCode;
    public CountryInfo? CountryInfo { get; internal set; }

    public override bool Equals(object? obj)
    {
      if (obj is Country country)
        return IsoCode == country.IsoCode;
      return false;
    }

    public bool Equals(Country? country)
    {
      return country != null && IsoCode == country.IsoCode;
    }

    public override int GetHashCode()
    {
      return IsoCode.GetHashCode();
    }
  }

  /// <summary>
  /// Storage class for country/exchange holidays.
  /// </summary>
  public partial class Holiday : DataObject, IEquatable<Holiday>, ICloneable, IUpdateable<Holiday>
  {
    public Holiday(Guid id, Attributes attributeSet, string tag, Guid parentId, string name, HolidayType type, Months month, int dayOfMonth, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday): base(id, attributeSet, tag)
    {
      Type = type;
      ParentId = parentId;
      Name = name;
      Month = month;
      DayOfMonth = dayOfMonth;
      DayOfWeek = dayOfWeek;
      WeekOfMonth = weekOfMonth;
      MoveWeekendHoliday = moveWeekendHoliday;
    }

    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private HolidayType m_type;
    [ObservableProperty] private string m_name;
    [ObservableProperty] private Months m_month;
    [ObservableProperty] private int m_dayOfMonth;
    [ObservableProperty] private DayOfWeek m_dayOfWeek;
    [ObservableProperty] private WeekOfMonth m_weekOfMonth;
    [ObservableProperty] private MoveWeekendHoliday m_moveWeekendHoliday;

    public bool Equals(Holiday? other)
    {
      //compare most fields but we don't care about the name
      return other != null &&
             other.ParentId == ParentId &&
             other.Month == Month &&
             other.DayOfMonth == DayOfMonth &&
             other.DayOfWeek == DayOfWeek &&
             other.WeekOfMonth == WeekOfMonth &&
             other.MoveWeekendHoliday == MoveWeekendHoliday;
    }

    public object Clone()
    {
      return new Holiday(Id, AttributeSet, TagStr, ParentId, Name, Type, Month, DayOfMonth, DayOfWeek, WeekOfMonth, MoveWeekendHoliday);
    }

    public void Update(Holiday item)
    {
      AttributeSet = item.AttributeSet;
      TagStr = item.TagStr;
      ParentId = item.ParentId;
      Name = item.Name;
      Type = item.Type;
      Month = item.Month;
      DayOfMonth = item.DayOfMonth;
      DayOfWeek = item.DayOfWeek;
      WeekOfMonth = item.WeekOfMonth;
      MoveWeekendHoliday = item.MoveWeekendHoliday;
    }

    public DateOnly ForYear(int year)
    {
      switch (Type)
      {
        case HolidayType.DayOfMonth:
          return getDayOfMonth(year, Month, DayOfMonth);
        case HolidayType.DayOfWeek:
          return getNthDayOfNthWeek(year, Month, DayOfWeek, WeekOfMonth);
        default:
          throw new NotImplementedException("Unknown holiday type.");
      }
    }

    /// <summary>
    /// Adjust computed holiday if it is required for holidays falling over the weekend. 
    /// </summary>
    protected DateTime adjustForWeekend(DateTime dateTime)
    {
      DateTime result = dateTime;

      if (MoveWeekendHoliday != MoveWeekendHoliday.DontAdjust)
      {
        switch (result.DayOfWeek)
        {
          //TBD: This will not always work correctly if you have holidays close together, it will move the holiday to the previous Friday or next Monday
          //     but if those days are also holidays then it will be incorrect. For now this solution will do, to fix it you'd need to have the notion
          //     of a holiday calendar that is a parent of this holiday object and this holiday object can then refer to it to get the other holidays to
          //     adjust itself correctly.
          case DayOfWeek.Saturday:
            if (MoveWeekendHoliday == MoveWeekendHoliday.PreviousBusinessDay)
              result = result.AddDays(-1);
            else //NextBusinessDay
              result = result.AddDays(2);
            break;
          case DayOfWeek.Sunday:
            if (MoveWeekendHoliday == MoveWeekendHoliday.PreviousBusinessDay)
              result = result.AddDays(-2);
            else //NextMonday
              result = result.AddDays(1);
            break;
        }
      }

      return result;
    }

    /// <summary>
    /// Returns the specific day of the month adjusted for weekends.
    /// </summary>
    protected DateOnly getDayOfMonth(int year, Months month, int day)
    {
      DateTime result = new DateTime(year, (int)month, day);
      result = adjustForWeekend(result);
      return DateOnly.FromDateTime(result);
    }

    /// <summary>
    /// Specify which day of which week of a month and this function will get the date 
    /// this function uses the month and year of the date provided
    /// </summary>
    protected DateOnly getNthDayOfNthWeek(int year, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth)
    {
      DateTime firstOfMonth = new DateTime(year, (int)month, 1); //get first date of month
      DateTime result = firstOfMonth.AddDays(6 - (double)firstOfMonth.AddDays(-((int)dayOfWeek + 1)).DayOfWeek); //get first dayOfWeek of month
      result = result.AddDays(((int)weekOfMonth - 1) * 7);  //get the correct week
      if (result >= firstOfMonth.AddMonths(1)) result = result.AddDays(-7);   //if day is past end of month then adjust backwards a week
      result = adjustForWeekend(result);
      return DateOnly.FromDateTime(result);
    }
  }

  /// <summary>
  /// Storage class for exchange data.
  /// </summary>
  public partial class Exchange : DataObject, IEquatable<Exchange>, ICloneable, IUpdateable<Exchange>
  {
    /// <summary>
    /// Logo path representing the blank logo for exchanges that do not yet have a logo assignment.
    /// </summary>
    public static Guid BlankLogoId { get => Guid.Empty; }
    private static string s_blankLogoPath = GetLogoPath(BlankLogoId);
    public static string BlankLogoPath { get => s_blankLogoPath;  }

    /// <summary>
    /// International Exchange used as the default parent for all unassigned children.
    /// </summary>
    private static string s_internationalIdStr = "11111111-1111-1111-1111-111111111111";
    private static Guid s_internationalId = Guid.Parse(s_internationalIdStr);
    public static Guid InternationalId { get => s_internationalId; }
    private static string s_internationalLogoPath = GetLogoPath(s_internationalId);
    public static string InternationalLogoPath { get => s_internationalLogoPath; }

    /// <summary>
    /// Create the logo path to use for logo with a given id and extension.
    /// </summary>
    public static string CreateLogoPath(Guid logoId, string extension)
    {
      string internalExtension = extension;
      internalExtension = internalExtension.Replace('.', ' ');
      internalExtension = internalExtension.Trim();
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");
      return $"{tradeSharpHome}\\data\\assets\\exchangelogos\\{logoId.ToString()}.{internalExtension}";
    }

    /// <summary>
    /// Retrieves the logo path for an exchange logo.
    /// </summary>
    public static string GetLogoPath(Guid logoId)
    {
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");

      //use fallback based on different image types, use png when possible since it supports proper transparency
      string logoFilename = $"{tradeSharpHome}\\data\\assets\\exchangelogos\\{logoId.ToString()}.png";
      if (File.Exists(logoFilename)) return logoFilename;
      logoFilename = $"{tradeSharpHome}\\data\\assets\\exchangelogos\\{logoId.ToString()}.jpg";
      if (File.Exists(logoFilename)) return logoFilename;
      logoFilename = $"{tradeSharpHome}\\data\\assets\\exchangelogos\\{logoId.ToString()}.jpeg";
      if (File.Exists(logoFilename)) return logoFilename;
      return $"{tradeSharpHome}\\data\\assets\\exchangelogos\\{Guid.Empty.ToString()}.png"; //return no logo image
    }

    /// <summary>
    /// Replaces the exchange logo with the next given logo image.
    /// </summary>
    public static void ReplaceLogo(Exchange exchange, string newLogoImagePath)
    {
      if (exchange.LogoPath != Exchange.BlankLogoPath && File.Exists(exchange.LogoPath)) File.Delete(exchange.LogoPath);    //ensure that we do not keep stale file around since new file extension can be different from current file extension
      exchange.LogoId = Guid.NewGuid();   //NOTE: We need to update the logo Id to get a new logo path otherwise bindings to it would not update in the UI
      exchange.LogoPath = Exchange.CreateLogoPath(exchange.LogoId, Path.GetExtension(newLogoImagePath));
      File.Copy(newLogoImagePath, exchange.LogoPath);
    }

    public Exchange(Guid id, Attributes attributeSet, string tag, Guid countryId, string name, IList<string> alternateNames, TimeZoneInfo timeZone, int defaultPriceDecimals, int defaultMinimumMovement, int defaultBigPointValue, Guid logoId, string url): base(id, attributeSet, tag)
    {
      CountryId = countryId;
      Name = name;
      AlternateNames = new ObservableCollection<string>(alternateNames);
      TimeZone = timeZone;
      DefaultPriceDecimals = defaultPriceDecimals;
      DefaultMinimumMovement = defaultMinimumMovement;
      DefaultBigPointValue = defaultBigPointValue;
      LogoId = logoId;
      LogoPath = GetLogoPath(logoId);
      Url = url;
    }

    [ObservableProperty] private Guid m_countryId;
    [ObservableProperty] private string m_name;
    public ObservableCollection<string> AlternateNames { get; set; }
    [ObservableProperty] private TimeZoneInfo m_timeZone;
    [ObservableProperty] private int m_defaultPriceDecimals;
    public double DefaultPriceScale { get => 1 / Math.Pow(10, DefaultPriceDecimals); }
    [ObservableProperty] private int m_defaultMinimumMovement;
    [ObservableProperty] private int m_defaultBigPointValue;
    [ObservableProperty] private Guid m_logoId; //logo Id is used for filename under assets\exchangeLogos
    [ObservableProperty] private string m_logoPath;
    [ObservableProperty] private string m_url;

    public bool Equals(Exchange? other)
    {
      return other != null && other.Id == Id;
    }

    public object Clone()
    {
      return new Exchange(Id, AttributeSet, TagStr, CountryId, Name, AlternateNames, TimeZone, DefaultPriceDecimals, DefaultMinimumMovement, DefaultBigPointValue, LogoId, Url);
    }

    public void Update(Exchange item)
    {
      AttributeSet = item.AttributeSet;
      TagStr = item.TagStr;
      CountryId = item.CountryId;
      Name = item.Name;
      AlternateNames.Clear();
      foreach (var name in item.AlternateNames) AlternateNames.Add(name);
      TimeZone = item.TimeZone;
      DefaultPriceDecimals = item.DefaultPriceDecimals;
      DefaultMinimumMovement = item.DefaultMinimumMovement;
      DefaultBigPointValue = item.DefaultBigPointValue;
      LogoId = item.LogoId;
      Url = item.Url;
    }
  }

  /// <summary>
  /// Storage class for exchange session data.
  /// </summary>
  public partial class Session : DataObject, IEquatable<Session>, ICloneable, IUpdateable<Session>
  {
    public Session(Guid id, Attributes attributeSet, string tag, string name, Guid exchangeId, DayOfWeek dayOfWeek, TimeOnly start, TimeOnly end): base(id, attributeSet, tag)
    {
      Name = name;
      ExchangeId = exchangeId;
      DayOfWeek = dayOfWeek;
      Start = start;
      End = end;
    }

    [ObservableProperty] private string m_name;
    [ObservableProperty] private Guid m_exchangeId;
    [ObservableProperty] private DayOfWeek m_dayOfWeek;
    [ObservableProperty] private TimeOnly m_start;
    [ObservableProperty] private TimeOnly m_end;

    public bool Equals(Session? other)
    {
      return other != null && other.Id == Id;
    }

    public object Clone()
    {
      return new Session(Id, AttributeSet, TagStr, Name, ExchangeId, DayOfWeek, Start, End);
    }

    public void Update(Session item)
    {
      AttributeSet = item.AttributeSet;
      TagStr = item.TagStr;
      Name = item.Name;
      ExchangeId = item.ExchangeId;
      DayOfWeek = item.DayOfWeek;
      Start = item.Start;
      End = item.End;
    }
  }

  /// <summary>
  /// Storage base class for instrument data, specializations will extend this and use extended properties to save additional field data into the database.
  /// Good sources to download instrument definitions
  /// Nasdaq - https://www.nasdaq.com/market-activity/stocks/screener
  /// InteractiveBrokers - https://www.interactivebrokers.com/en/trading/products-exchanges.php#/productTypes=STK
  /// </summary>
  public partial class Instrument : DataObject, ICloneable, IUpdateable<Instrument>, IComparable
  {
    //constants
    /// <summary>
    /// Defaults for basic instrument price movements.
    /// </summary>
    public const int DefaultPriceDecimals = 2;
    public const int DefaultMinimumMovement = 1;
    public const int DefaultBigPointValue = 1;

    //enums


    //types


    //attributes


    //constructors
    public Instrument(string ticker, Attributes attributeSet, string tag, InstrumentType type, IList<string> alternateTickers, string name, string description, DateTime inceptionDate, int priceDecimals, int minimumMovement, int bigPointValue, Guid primaryExhangeId, IList<Guid> secondaryExchangeIds, string extendedProperties) : base(Guid.Empty, attributeSet, tag)
    {
      Type = type;
      Ticker = ticker;
      AlternateTickers = alternateTickers;
      Name = name;
      Description = description;
      InceptionDate = inceptionDate;
      PriceDecimals = priceDecimals;
      MinimumMovement = minimumMovement;
      BigPointValue = bigPointValue;
      PrimaryExchangeId = primaryExhangeId;
      SecondaryExchangeIds = secondaryExchangeIds;
      m_extendedProperties = extendedProperties;    //JSON data for dynamic extended properties
    }

    //finalizers


    //properties
    public new Guid Id { get => throw new NotImplementedException("Id not supported, use Ticker as key"); set => throw new NotImplementedException("Id not supported, use Ticker as key"); }   //hide the base class Id property, instruments uses the ticker as key
    [ObservableProperty] private InstrumentType m_type;
    [ObservableProperty] private string m_ticker;
    [ObservableProperty] private IList<string> m_alternateTickers;
    [ObservableProperty] private string m_name;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private DateTime m_inceptionDate;
    [ObservableProperty] private int m_priceDecimals;
    public double PriceScale { get => 1 / Math.Pow(10, PriceDecimals); }
    [ObservableProperty] private int m_minimumMovement;
    [ObservableProperty] private int m_bigPointValue;
    [ObservableProperty] private Guid m_primaryExchangeId;
    [ObservableProperty] private string m_extendedProperties;
    [ObservableProperty] private IList<Guid> m_secondaryExchangeIds;

    //methods
    public override bool Equals(object? other)
    {
      if (other != null)
      {
        if (other is Instrument instrument)
        {
          if (Ticker == instrument.Ticker) return true;
          if (AlternateTickers.FirstOrDefault(t => t == instrument.Ticker) != null) return true;
          if (instrument.AlternateTickers.FirstOrDefault(t => t == Ticker) != null) return true;
        }
        else if (other is string ticker)
        {
          ticker = ticker.ToUpper();
          if (Ticker == ticker) return true;
          if (AlternateTickers.FirstOrDefault(t => t == ticker) != null) return true;
        }
      }

      return false;
    }

    public override int GetHashCode()
    {
      return Ticker.GetHashCode() + AlternateTickers.GetHashCode();
    }

    public object Clone()
    {
      return new Instrument(Ticker, AttributeSet, TagStr, Type, new List<string>(AlternateTickers), Name, Description, InceptionDate, PriceDecimals, MinimumMovement, BigPointValue, PrimaryExchangeId, new List<Guid>(SecondaryExchangeIds), ExtendedProperties);
    }

    public void Update(Instrument item)
    {
      AttributeSet = item.AttributeSet;
      TagStr = item.TagStr;
      Type = item.Type;
      Ticker = item.Ticker;
      AlternateTickers = item.AlternateTickers;
      Name = item.Name;
      Description = item.Description;
      InceptionDate = item.InceptionDate;
      PriceDecimals = item.PriceDecimals;
      MinimumMovement = item.MinimumMovement;
      BigPointValue = item.BigPointValue;
      PrimaryExchangeId = item.PrimaryExchangeId;
      SecondaryExchangeIds = item.SecondaryExchangeIds;
      ExtendedProperties = item.ExtendedProperties;
    }

    public int CompareTo(object? o)
    {
      if (o == null || !(o is Instrument)) return 1;
      Instrument instrument = (Instrument)o;
      return Ticker.CompareTo(instrument.Ticker);
    }

  }

  /// <summary>
  /// Stock instrument implementation to add additional stock specific properties.
  /// </summary>
  public partial class Stock : Instrument {
    //constants
    public double MarketCapInvalid = -1;
    public double FiftyTwoWeekHighInvalid = -1;
    public double FiftyTwoWeekLowInvalid = -1;

    //enums


    //types


    //attributes


    //properties
    [ObservableProperty] double m_marketCap;
    [ObservableProperty] double m_sharesOutstanding;
    [ObservableProperty] double m_employeeCount;
    [ObservableProperty] string m_address;
    [ObservableProperty] string m_city;
    [ObservableProperty] string m_state;
    [ObservableProperty] string m_zip;
    [ObservableProperty] string m_phoneNumber;
    [ObservableProperty] string m_url;
    [ObservableProperty] double m_fiftyTwoWeekHigh;
    [ObservableProperty] double m_fiftyTwoWeekLow;

    //constructors
    public Stock(string ticker, Attributes attributeSet, string tag, InstrumentType type, IList<string> alternateTickers, string name, string description, DateTime inceptionDate, int priceDecimals, int minimumMovement, int bigPointValue, Guid primaryExhangeId, IList<Guid> secondaryExchangeIds, string extendedProperties): base(ticker, attributeSet, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, primaryExhangeId, secondaryExchangeIds, extendedProperties) 
    {
      MarketCap = MarketCapInvalid;
      FiftyTwoWeekHigh = FiftyTwoWeekHighInvalid;
      FiftyTwoWeekLow = FiftyTwoWeekLowInvalid;
      Address = string.Empty;
      City = string.Empty;
      State = string.Empty;
      Zip = string.Empty;
      PhoneNumber = string.Empty;
      Url = string.Empty;
    }

    //finalizers


    //interface implementations


    //methods


  }

  /// <summary>
  /// Storage class for general instrument grouping definition.
  /// </summary>
  public partial class InstrumentGroup : DataObject, IEquatable<InstrumentGroup>, ICloneable, IUpdateable<InstrumentGroup>
  {
    public static Guid InstrumentGroupRoot = Guid.Parse("11111111-1111-1111-1111-111111111111");    //define any non-null Guid to be used as the root group

    public InstrumentGroup(Guid id, Attributes attributeSet, string tag, Guid parentId, string name, IList<string> alternateNames, string description, string userId, IList<string> instruments): base(id, attributeSet, tag)
    {
      ParentId = parentId;
      Name = name;
      AlternateNames = new ObservableCollection<string>(alternateNames);
      Description = description;
      UserId = userId;
      Instruments = instruments;
    }

    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private string m_name;
    public ObservableCollection<string> AlternateNames;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private string m_userId;   //specific Id to be used by the user, can be used in data file exports/imports to identify the group
    [ObservableProperty] private IList<string> m_instruments; //instruments associated with the group

    public override bool Equals(object? other)
    {
      if (other != null)
      {
        if (other is InstrumentGroup instrumentGroup)
        {
          if (instrumentGroup.Id == Id) return true;
          if (instrumentGroup.UserId.ToUpper() == UserId.ToUpper()) return true;
          if (instrumentGroup.Name.ToUpper() == Name.ToUpper()) return true;
          //perform two way comparison for alternate names
          if (instrumentGroup.AlternateNames.FirstOrDefault(t => t.ToUpper() == Name.ToUpper()) != null) return true;
          if (AlternateNames.FirstOrDefault(t => t.ToUpper() == instrumentGroup.Name.ToUpper()) != null) return true;
        }
        else if (other is string name)
        {
          name = name.ToUpper();
          if (Name.ToUpper() == name) return true;
          if (AlternateNames.FirstOrDefault(t => t.ToUpper() == name) != null) return true;
        }
      }

      return false;
    }

    public override int GetHashCode()
    {
      return Name.GetHashCode() + AlternateNames.GetHashCode();
    }

    public bool Equals(InstrumentGroup? other)
    {
      if (other == null) return false;
      if (other == this) return true;
      if (other.Id == Id) return true;
      string nameUpper = Name.ToUpper();
      string otherUpper = other.Name.ToUpper();
      if (otherUpper == nameUpper) return true;
      if (other.AlternateNames.FirstOrDefault(x => x.ToUpper() == nameUpper) != null) return true;
      if (AlternateNames.FirstOrDefault(x => x.ToUpper() == otherUpper) != null) return true;
      return false;
    }

    public object Clone()
    {
      return new InstrumentGroup(Id, AttributeSet, TagStr, ParentId, Name, new List<string>(AlternateNames), Description, UserId, new List<string>(Instruments));
    }

    public void Update(InstrumentGroup item)
    {
      AttributeSet = item.AttributeSet;
      TagStr = item.TagStr;
      ParentId = item.ParentId;
      Name = item.Name;
      AlternateNames = item.AlternateNames;
      Description = item.Description;
      Instruments = item.Instruments;
    }
  }

  /// <summary>
  /// Storage structure for fundamental definitions.
  /// </summary>
  public partial class Fundamental : DataObject, IEquatable<Fundamental>, ICloneable, IUpdateable<Fundamental>
  {
    public Fundamental(Guid id, Attributes attributeSet, string tag, string name, string description, FundamentalCategory category, FundamentalReleaseInterval releaseInterval): base(id, attributeSet, tag)
    {
      Name = name;
      Description = description;
      Category = category;
      ReleaseInterval = releaseInterval;
    }

    [ObservableProperty] private string m_name;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private FundamentalCategory m_category;
    [ObservableProperty] private FundamentalReleaseInterval m_releaseInterval;

    public bool Equals(Fundamental? other)
    {
      return other != null && other.Id == Id;
    }

    public object Clone()
    {
      return new Fundamental(Id, AttributeSet, TagStr, Name, Description, Category, ReleaseInterval);
    }

    public void Update(Fundamental item)
    {
      AttributeSet = item.AttributeSet;
      TagStr = item.TagStr;
      Name = item.Name;
      Description = item.Description;
      Category = item.Category;
      ReleaseInterval = item.ReleaseInterval;
    }
  }

  /// <summary>
  /// Storage structure for country fundamental values.
  /// </summary>
  public partial class CountryFundamental : ObservableObject, IEquatable<CountryFundamental>, ICloneable, IUpdateable<CountryFundamental>
  {
    public CountryFundamental(string dataProviderName, Guid associationId, Guid fundamentalId, Guid countryId)
    {
      DataProviderName = dataProviderName;
      AssociationId = associationId;
      FundamentalId = fundamentalId;
      CountryId = countryId;
      Values = new List<Tuple<DateTime, double>>();
    }

    [ObservableProperty] private string m_DataProviderName;
    [ObservableProperty] private Guid m_AssociationId;
    [ObservableProperty] private Guid m_FundamentalId;
    [ObservableProperty] private Guid m_CountryId;
    [ObservableProperty] private IList<Tuple<DateTime, double>> m_values;

    public void AddValue(DateTime dateTime, double value) { Values.Add(new Tuple<DateTime, double>(dateTime, value)); }

    public bool Equals(CountryFundamental? other)
    {
      return other != null && other.AssociationId == AssociationId;
    }

    public object Clone()
    {
      return new CountryFundamental(DataProviderName, AssociationId, FundamentalId, CountryId);
    }

    public void Update(CountryFundamental item)
    {
      DataProviderName = item.DataProviderName;
      FundamentalId = item.FundamentalId;
      CountryId = item.CountryId;
    }
  }

  /// <summary>
  /// Storage structure for instrument fundamental values.
  /// </summary>
  public partial class InstrumentFundamental : ObservableObject, IEquatable<InstrumentFundamental>, ICloneable, IUpdateable<InstrumentFundamental>
  {
    public InstrumentFundamental(string dataProviderName, Guid associationId, Guid fundamentalId, string instrumentTicker)
    {
      DataProviderName = dataProviderName;
      AssociationId = associationId;
      FundamentalId = fundamentalId;
      InstrumentTicker = instrumentTicker;
      Values = new List<Tuple<DateTime, double>>();
    }

    [ObservableProperty] private string m_DataProviderName;
    [ObservableProperty] private Guid m_AssociationId;
    [ObservableProperty] private Guid m_FundamentalId;
    [ObservableProperty] private string m_InstrumentTicker;
    [ObservableProperty] private IList<Tuple<DateTime, double>> m_values;

    public void AddValue(DateTime dateTime, double value) { Values.Add(new Tuple<DateTime, double>(dateTime, value)); }

    public bool Equals(InstrumentFundamental? other)
    {
      return other != null && other.AssociationId == AssociationId;
    }

    public object Clone()
    {
      return new InstrumentFundamental(DataProviderName, AssociationId, FundamentalId, InstrumentTicker);
    }

    public void Update(InstrumentFundamental item)
    {
      DataProviderName = item.DataProviderName;
      FundamentalId = item.FundamentalId;
      InstrumentTicker = item.InstrumentTicker;
    }
  }

  //TBD: Determine whether it is necessary to make these objects observable since it might become super slow.

  /// <summary>
  /// Represents the data cache around price bar data.
  /// </summary>
  public class DataCacheBars
  {
    public DataCacheBars(int count)
    {
      Count = count;
      DateTime = new DateTime[count];
      Open = new double[count];
      High = new double[count];
      Low = new double[count];
      Close = new double[count];
      Volume = new double[count];
    }

    public int Count;
    public IList<DateTime> DateTime;
    public IList<double> Open;
    public IList<double> High;
    public IList<double> Low;
    public IList<double> Close;
    public IList<double> Volume;
  }

  /// <summary>
  /// Represents the data cache around level 1 market data (tick data).
  /// NOTE: The underlying List type could become a problem if huge amounts of tick data is processed per instrument, in that
  ///       case the underlying storage structure would need to be adjusted - would probably need some custom memory implementation
  ///       that implements the IList interface.
  /// </summary>
  public class DataCacheLevel1
  {
    public DataCacheLevel1(int count)
    {
      Count = count;
      DateTime = new DateTime[count];
      Bid = new double[count];
      BidSize = new double[count];
      Ask = new double[count];
      AskSize = new double[count];
      Last = new double[count];
      LastSize = new double[count];
    }

    public int Count;
    public IList<DateTime> DateTime;
    public IList<double> Bid;
    public IList<double> BidSize;
    public IList<double> Ask;
    public IList<double> AskSize;
    public IList<double> Last;
    public IList<double> LastSize;
  }

  /// <summary>
  /// Generic cache entry used by the DataManager to store loaded/cached data.
  /// </summary>
  public class DataCache
  {
    public DataCache(string dataProviderName, string instrumentTicker, Resolution resolution, DateTime from, DateTime to, int count)
    {
      DataProviderName = dataProviderName;
      InstrumentTicker = instrumentTicker;
      Resolution = resolution;
      From = from;
      To = to;
      Count = count;
      Resolution = resolution;
      Data = default!;  //null forgiving, will create data below

      switch (Resolution)
      {
        //candlestick data
        case Resolution.Minutes:
        case Resolution.Hours:
        case Resolution.Days:
        case Resolution.Weeks:
        case Resolution.Months:
          Data = new DataCacheBars(count);
          break;
        //tick data
        case Resolution.Level1:
          Data = new DataCacheLevel1(count);
          break;
      }
    }

    public string DataProviderName { get; }
    public string InstrumentTicker { get; }
    public Resolution Resolution { get; }
    public DateTime From { get; }
    public DateTime To { get; }
    public int Count { get; set; }
    public object Data { get; set; } //data stored based on the Resolution field
  }

  /// <summary>
  /// Interface to be implemented by objects used to store data. The general design is a Bridge pattern where the implementing class
  /// would serve as a bridge between a DataManager and an underlying storage technology like a database. The API for the data store
  /// focusses on the structure of the underlying data while the data manager layer would add object oriented functionality with the
  /// necessary locking to ensure data consistency.
  /// 
  /// The data store should also take into account specific configuration settings related to data, e.g. data related to timezones
  /// should be correctly stored in UTC time and then returned using the user's desired TimeZone from the IConfiguration interface.
  /// </summary>
  public interface IDatabase
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IList<Resolution> SupportedDataResolutions { get; }
    bool IsOptimizing { get; }

    /// <summary>
    /// Utility functions.
    /// </summary>
    void StartTransaction();
    void EndTransaction(bool success);
    void Optimize();

    /// <summary>
    /// Country definition interface.
    /// </summary>
    void CreateCountry(Country country);
    void UpdateCountry(Country country);
    Country? GetCountry(Guid id);
    IList<Country> GetCountries();
    int DeleteCountry(Guid id);

    /// <summary>
    /// Exchange definition interface.
    /// </summary>
    void CreateExchange(Exchange exchange);
    Exchange? GetExchange(Guid id);
    IList<Exchange> GetExchanges();
    void UpdateExchange(Exchange exchange);
    int DeleteExchange(Guid id);

    /// <summary>
    /// Country and exchange holiday inerface.
    /// </summary>
    void CreateHoliday(Holiday holiday);
    Holiday? GetHoliday(Guid id);
    IList<Holiday> GetHolidays(Guid parentId);
    IList<Holiday> GetHolidays();
    void UpdateHoliday(Holiday holiday);
    int DeleteHoliday(Guid id);

    /// <summary>
    /// Create/update a trading session definition on a given PrimaryExchange.
    /// </summary>
    void CreateSession(Session session);
    Session? GetSession(Guid id);
    IList<Session> GetSessions(Guid exchangeId);
    IList<Session> GetSessions();
    void UpdateSession(Session session);
    int DeleteSession(Guid id);

    /// <summary>
    /// Instrument group definition interface.
    /// </summary>
    void CreateInstrumentGroup(InstrumentGroup instrumentGroup);
    void CreateInstrumentGroupInstrument(Guid instrumentGroupId, string instrumentTicker);
    InstrumentGroup? GetInstrumentGroup(Guid id);
    IList<InstrumentGroup> GetInstrumentGroups();
    IList<string> GetInstrumentGroupInstruments(Guid instrumentGroupId);
    void UpdateInstrumentGroup(InstrumentGroup instrumentGroup);
    int DeleteInstrumentGroup(Guid id);
    int DeleteInstrumentGroupChild(Guid parentId, Guid childId);
    int DeleteInstrumentGroupInstrument(Guid instrumentGroupId, string instrumentTicker);

    /// <summary>
    /// Instrument definition interface.
    /// </summary>
    void CreateInstrument(Instrument instrument);
    void AddInstrumentToExchange(string instrumentTicker, Guid exchange);
    int GetInstrumentCount();
    int GetInstrumentCount(InstrumentType instrumentType);
    int GetInstrumentCount(string tickerFilter, string nameFilter, string descriptionFilter); //no filter if filters are blank, filters should support wildcards * and ?
    int GetInstrumentCount(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter); //no filter if filters are blank, filters should support wildcards * and ?
    IList<Instrument> GetInstruments();
    IList<Instrument> GetInstruments(InstrumentType instrumentType);
    IList<Instrument> GetInstruments(string tickerFilter, string nameFilter, string descriptionFilter);
    IList<Instrument> GetInstruments(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter);
    IList<Instrument> GetInstrumentsOffset(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
    IList<Instrument> GetInstrumentsOffset(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
    IList<Instrument> GetInstrumentsPage(string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize);  //paged loading of instruments
    IList<Instrument> GetInstrumentsPage(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize);  //paged loading of instruments
    Instrument? GetInstrument(string ticker);
    void UpdateInstrument(Instrument instrument);
    int DeleteInstrument(Instrument instrument);
    int DeleteInstrumentFromExchange(string instrumentTicker, Guid exchangeId);

    /// <summary>
    /// Fundamental definition interface.
    /// </summary>
    void CreateFundamental(Fundamental fundamental);
    IList<Fundamental> GetFundamentals();

    //UPDATE??

    int DeleteFundamental(Guid id);
    int DeleteFundamentalValues(Guid id);
    int DeleteFundamentalValues(string dataProviderName, Guid id);

    void CreateCountryFundamental(CountryFundamental fundamental);
    IList<CountryFundamental> GetCountryFundamentals(string dataProviderName);
    void UpdateCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime, double value);
    int DeleteCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId);
    int DeleteCountryFundamentalValue(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime);

    void CreateInstrumentFundamental(InstrumentFundamental fundamental);
    IList<InstrumentFundamental> GetInstrumentFundamentals(string dataProviderName);
    void UpdateInstrumentFundamental(string dataProviderName, Guid fundamentalId, string instrumentTicker, DateTime dateTime, double value);
    int DeleteInstrumentFundamental(string dataProviderName, Guid fundamentalId, string instrumentTicker);
    int DeleteInstrumentFundamentalValue(string dataProviderName, Guid fundamentalId, string instrumentTicker, DateTime dateTime);

    /// <summary>
    /// Create/update price data from a given DataProvider for a given instrument. Paged functions are provided to allow incremental loading of large amounts of data.
    /// </summary>
    void UpdateData(string dataProviderName, string ticker, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, double volume);
    void UpdateData(string dataProviderName, string ticker, DateTime dateTime, double bid, double bidSize, double ask, double askSize, double last, double lastSize);
    void UpdateData(string dataProviderName, string ticker, Resolution resolution, DataCacheBars bars);
    void UpdateData(string dataProviderName, string ticker, Resolution resolution, IList<IBarData> bars);
    void UpdateData(string dataProviderName, string ticker, Resolution resolution, IList<ILevel1Data> bars);
    void UpdateData(string dataProviderName, string ticker, DataCacheLevel1 bars);

    int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime dateTime);
    int DeleteData(string dataProviderName, string ticker, Resolution? resolution = null, DateTime? from = null, DateTime? to = null);

    int GetDataCount(string dataProviderName, string ticker, Resolution resolution);
    int GetDataCount(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to);
    IBarData? GetBarData(string dataProviderName, string ticker, Resolution resolution, DateTime dateTime, string priceFormatMask);
    IList<IBarData> GetBarData(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to, string priceFormatMask);
    IList<IBarData> GetBarData(string dataProviderName, string ticker, Resolution resolution, int index, int count, string priceFormatMask);
    IList<IBarData> GetBarData(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to, int index, int count, string priceFormatMask);

    ILevel1Data? GetLevel1Data(string dataProviderName, string ticker, DateTime dateTime, string priceFormatMask);
    //TODO: Add Level1 paging functions similar to the GetBarData ones above.
    IList<ILevel1Data> GetLevel1Data(string dataProviderName, string ticker, DateTime from, DateTime to, string priceFormatMask);
    DataCache GetDataCache(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to);
  }
}