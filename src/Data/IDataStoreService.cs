using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Common;
using static TradeSharp.Data.Country;

namespace TradeSharp.Data
{
  /// This file defines the basic interface to be maintained by data services used to store data for the trading system.

  /// <summary>
  /// Price data types to return for price queries.
  /// </summary>
  public enum PriceDataType
  {
    Actual,
    Synthetic,
    Both
  }

  /// <summary>
  /// Type of a tradeable instrument.
  /// </summary>
  public enum InstrumentType
  {
    None = 0,   //only used for initialization
    Stock,
    Forex,
    Crypto,
    Future,
    Option,
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
  /// Base class for data store service objects.
  /// </summary>
  public partial class DataObject : ObservableObject
  {
    public const Attributes DefaultAttributeSet = Attributes.Editable | Attributes.Deletable;

    public DataObject(Guid id, Attributes attributeSet)
    {
      Id = id;
      AttributeSet = attributeSet;
    }

    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private Attributes m_attributeSet;

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
    private static string s_internationalIsoCode = CountryInfo.InternationalId;
    public static string InternationalIsoCode { get => s_internationalIsoCode; } //three letter iso codes use alphabetical characters so using numbers should be good

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

    public Country(Guid id, Attributes attributeSet, string isoCode) : base(id, attributeSet)
    {
      IsoCode = isoCode;
      CountryInfo = CountryInfo.GetCountryInfo(IsoCode);
    }

    [ObservableProperty] private string m_isoCode;
    public CountryInfo? CountryInfo { get; internal set; }
    bool IEquatable<Country>.Equals(Country? country)
    {
      return country != null && IsoCode == country.IsoCode;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Id, IsoCode);
    }
  }

  /// <summary>
  /// Storage class for country/exchange holidays.
  /// </summary>
  public partial class Holiday : DataObject, IEquatable<Holiday>, ICloneable, IUpdateable<Holiday>
  {
    public Holiday(Guid id, Attributes attributeSet, Guid parentId, string name, HolidayType type, Months month, int dayOfMonth, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday): base(id, attributeSet)
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
      return new Holiday(Id, AttributeSet, ParentId, Name, Type, Month, DayOfMonth, DayOfWeek, WeekOfMonth, MoveWeekendHoliday);
    }

    public void Update(Holiday item)
    {
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
          //TBD: This will not always work correctly if you have holidays close together, it will move the holiday to the previsou Friday or next Monday
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
    private static string s_blankLogoPath = GetLogoPath(Guid.Empty);
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

    public Exchange(Guid id, Attributes attributeSet, Guid countryId, string name, TimeZoneInfo timeZone, Guid logoId): base(id, attributeSet)
    {
      CountryId = countryId;
      Name = name;
      TimeZone = timeZone;
      LogoId = logoId;
      LogoPath = GetLogoPath(logoId);
    }

    [ObservableProperty] private Guid m_countryId;
    [ObservableProperty] private string m_name;
    [ObservableProperty] private TimeZoneInfo m_timeZone;
    [ObservableProperty] private Guid m_logoId; //logo Id is used for filename under assets\exchangeLogos
    [ObservableProperty] private string m_logoPath;

    public bool Equals(Exchange? other)
    {
      return other != null && other.Id == Id;
    }

    public object Clone()
    {
      return new Exchange(Id, AttributeSet, CountryId, Name, TimeZone, LogoId);
    }

    public void Update(Exchange item)
    {
      CountryId = item.CountryId;
      Name = item.Name;
      TimeZone = item.TimeZone;
      LogoId = item.LogoId;
    }
  }

  /// <summary>
  /// Storage class for exchange session data.
  /// </summary>
  public partial class Session : DataObject, IEquatable<Session>, ICloneable, IUpdateable<Session>
  {
    public Session(Guid id, Attributes attributeSet, string name, Guid exchangeId, DayOfWeek dayOfWeek, TimeOnly start, TimeOnly end): base(id, attributeSet)
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
      return new Session(Id, AttributeSet, Name, ExchangeId, DayOfWeek, Start, End);
    }

    public void Update(Session item)
    {
      Name = item.Name;
      ExchangeId = item.ExchangeId;
      DayOfWeek = item.DayOfWeek;
      Start = item.Start;
      End = item.End;
    }
  }

  /// <summary>
  /// Storage base class for instrument data.
  /// </summary>
  public partial class Instrument : DataObject, IEquatable<Instrument>, ICloneable, IUpdateable<Instrument>
  {
    public Instrument(Guid id, Attributes attributeSet, InstrumentType type, string ticker, string name, string description, DateTime inceptionDate, IList<Guid> instrumentGroupId, Guid primaryExhangeId, IList<Guid> secondaryExchangeIds): base(id, attributeSet)
    {
      Type = type;
      Ticker = ticker;
      Name = name;
      Description = description;
      InceptionDate = inceptionDate;
      InstrumentGroupIds = instrumentGroupId;
      PrimaryExchangeId = primaryExhangeId;
      SecondaryExchangeIds = secondaryExchangeIds;
    }

    [ObservableProperty] private InstrumentType m_type;
    [ObservableProperty] private string m_ticker;
    [ObservableProperty] private string m_name;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private DateTime m_inceptionDate;
    [ObservableProperty] private IList<Guid> m_instrumentGroupIds;
    [ObservableProperty] private Guid m_primaryExchangeId;
    [ObservableProperty] private IList<Guid> m_secondaryExchangeIds;

    public bool Equals(Instrument? other)
    {
      return other != null && other.Id == Id;
    }

    public object Clone()
    {
      return new Instrument(Id, AttributeSet, Type, Ticker, Name, Description, InceptionDate, InstrumentGroupIds, PrimaryExchangeId, SecondaryExchangeIds);
    }

    public void Update(Instrument item)
    {
      Type = item.Type;
      Ticker = item.Ticker;
      Name = item.Name;
      Description = item.Description;
      InceptionDate = item.InceptionDate;
      InstrumentGroupIds = item.InstrumentGroupIds;
      PrimaryExchangeId = item.PrimaryExchangeId;
      SecondaryExchangeIds = item.SecondaryExchangeIds;
    }
  }

  /// <summary>
  /// Storage class for general instrument grouping definition.
  /// </summary>
  public partial class InstrumentGroup : DataObject, IEquatable<InstrumentGroup>, ICloneable, IUpdateable<InstrumentGroup>
  {
    public static Guid InstrumentGroupRoot = Guid.Empty;

    public InstrumentGroup(Guid id, Attributes attributeSet, Guid parentId, string name, string description, IList<Guid> instruments): base(id, attributeSet)
    {
      ParentId = parentId;
      Name = name;
      Description = description;
      Instruments = instruments;
    }

    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private string m_name;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private IList<Guid> m_instruments;

    bool IEquatable<InstrumentGroup>.Equals(InstrumentGroup? other)
    {
      return other != null && other.Id == Id;
    }

    public object Clone()
    {
      return new InstrumentGroup(Id, AttributeSet, ParentId, Name, Description, Instruments);
    }

    public void Update(InstrumentGroup item)
    {
      ParentId = item.ParentId;
      Name = item.Name;
      Description = item.Description;
      Instruments = item.Instruments;
    }
  }

  /// <summary>
  /// Storage structure for fundamental definitions.
  /// </summary>
  public partial class Fundamental : DataObject, IEquatable<Fundamental>, ICloneable, IUpdateable<Fundamental>
  {
    public Fundamental(Guid id, Attributes attributeSet, string name, string description, FundamentalCategory category, FundamentalReleaseInterval releaseInterval): base(id, attributeSet)
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
      return new Fundamental(Id, AttributeSet, Name, Description, Category, ReleaseInterval);
    }

    public void Update(Fundamental item)
    {
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
    public InstrumentFundamental(string dataProviderName, Guid associationId, Guid fundamentalId, Guid instrumentId)
    {
      DataProviderName = dataProviderName;
      AssociationId = associationId;
      FundamentalId = fundamentalId;
      InstrumentId = instrumentId;
      Values = new List<Tuple<DateTime, double>>();
    }

    [ObservableProperty] private string m_DataProviderName;
    [ObservableProperty] private Guid m_AssociationId;
    [ObservableProperty] private Guid m_FundamentalId;
    [ObservableProperty] private Guid m_InstrumentId;
    [ObservableProperty] private IList<Tuple<DateTime, double>> m_values;

    public void AddValue(DateTime dateTime, double value) { Values.Add(new Tuple<DateTime, double>(dateTime, value)); }

    public bool Equals(InstrumentFundamental? other)
    {
      return other != null && other.AssociationId == AssociationId;
    }

    public object Clone()
    {
      return new InstrumentFundamental(DataProviderName, AssociationId, FundamentalId, InstrumentId);
    }

    public void Update(InstrumentFundamental item)
    {
      DataProviderName = item.DataProviderName;
      FundamentalId = item.FundamentalId;
      InstrumentId = item.InstrumentId;
    }
  }


  //TBD: Determine whether it is necessary to make these objects observable since it might become super slow.

  /// <summary>
  /// Represents the data cache around price bar data.
  /// </summary>
  public class BarData
  {
    public BarData(int count)
    {
      Count = count;
      DateTime = new DateTime[count];
      Open = new double[count];
      High = new double[count];
      Low = new double[count];
      Close = new double[count];
      Volume = new long[count];
      Synthetic = new bool[count];
    }

    public int Count;
    public IList<DateTime> DateTime;
    public IList<double> Open;
    public IList<double> High;
    public IList<double> Low;
    public IList<double> Close;
    public IList<long> Volume;
    public IList<bool> Synthetic;
  }

  /// <summary>
  /// Represents the data cache around level 1 market data (tick data).
  /// NOTE: The underlying List type could become a problem if huge amounts of tick data is processed per instrument, in that
  ///       case the underlying storage structure would need to be adjusted - would probably need some custom memory implementation
  ///       that implements the IList interface.
  /// </summary>
  public class Level1Data

  {
    public Level1Data(int count)
    {
      Count = count;
      DateTime = new DateTime[count];
      Bid = new double[count];
      BidSize = new long[count];
      Ask = new double[count];
      AskSize = new long[count];
      Last = new double[count];
      LastSize = new long[count];
      Synthetic = new bool[count];
    }

    public int Count;
    public IList<DateTime> DateTime;
    public IList<double> Bid;
    public IList<long> BidSize;
    public IList<double> Ask;
    public IList<long> AskSize;
    public IList<double> Last;
    public IList<long> LastSize;
    public IList<bool> Synthetic;
  }

  /// <summary>
  /// Generic cache entry used by the DataManager to store loaded/cached data.
  /// </summary>
  public class DataCache
  {
    public DataCache(string dataProviderName, Guid instrumentId, Resolution resolution, PriceDataType priceDataType, DateTime from, DateTime to, int count)
    {
      DataProviderName = dataProviderName;
      InstrumentId = instrumentId;
      Resolution = resolution;
      PriceDataType = priceDataType;
      From = from;
      To = to;
      Count = count;
      Resolution = resolution;
      Data = default!;  //null forgiving, will create data below

      switch (Resolution)
      {
        //candlestick data
        case Resolution.Minute:
        case Resolution.Hour:
        case Resolution.Day:
        case Resolution.Week:
        case Resolution.Month:
          Data = new BarData(count);
          break;
        //tick data
        case Resolution.Level1:
          Data = new Level1Data(count);
          break;
      }
    }

    public string DataProviderName { get; }
    public Guid InstrumentId { get; }
    public Resolution Resolution { get; }
    public PriceDataType PriceDataType { get; }
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
  public interface IDataStoreService : IDisposable
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public IList<Resolution> SupportedDataResolutions { get; }

    /// <summary>
    /// Start a transaction if the data store supports transactional updates.
    /// </summary>
    void StartTransaction();

    /// <summary>
    /// End a transaction if the data store supports transactional updates.
    /// </summary>
    void EndTransaction(bool success);

    /// <summary>
    /// Country definition interface.
    /// </summary>
    public void CreateCountry(Country country);
    public Country? GetCountry(Guid id);
    public IList<Country> GetCountries();
    public int DeleteCountry(Guid id);

    /// <summary>
    /// Exchange definition interface.
    /// </summary>
    public void CreateExchange(Exchange exchange);
    public Exchange? GetExchange(Guid id);
    public IList<Exchange> GetExchanges();
    public void UpdateExchange(Exchange exchange);
    public int DeleteExchange(Guid id);

    /// <summary>
    /// Country and exchange holiday inerface.
    /// </summary>
    public void CreateHoliday(Holiday holiday);
    public Holiday? GetHoliday(Guid id);
    public IList<Holiday> GetHolidays(Guid parentId);
    public IList<Holiday> GetHolidays();
    public void UpdateHoliday(Holiday holiday);
    public int DeleteHoliday(Guid id);

    /// <summary>
    /// Create/update a trading session definition on a given PrimaryExchange.
    /// </summary>
    public void CreateSession(Session session);
    public Session? GetSession(Guid id);
    public IList<Session> GetSessions(Guid exchangeId);
    public IList<Session> GetSessions();
    public void UpdateSession(Session session);
    public int DeleteSession(Guid id);

    /// <summary>
    /// Instrument group definition interface.
    /// </summary>
    public void CreateInstrumentGroup(InstrumentGroup instrumentGroup);
    public void CreateInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId);
    public IList<InstrumentGroup> GetInstrumentGroups();
    public IList<Guid> GetInstrumentGroupInstruments(Guid instrumentGroupId);
    public void UpdateInstrumentGroup(Guid id, Guid parentId);
    public void UpdateInstrumentGroup(Guid id, string name, string description);
    public void DeleteInstrumentGroup(Guid id);
    public void DeleteInstrumentGroupChild(Guid parentId, Guid childId);
    public void DeleteInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId);

    /// <summary>
    /// Instrument definition interface.
    /// </summary>
    public void CreateInstrument(Instrument instrument);
    public void AddInstrumentToExchange(Guid instrument, Guid exchange);
    public IList<Instrument> GetInstruments();
    public IList<Instrument> GetInstruments(InstrumentType instrumentType);
    public void UpdateInstrument(Guid id, Guid exchangeId, string ticker, DateTime inceptionDate);
    public int DeleteInstrument(Guid id, string ticker);
    public int DeleteInstrumentFromExchange(Guid instrumentId, Guid exchangeId);

    /// <summary>
    /// Fundamental definition interface.
    /// </summary>
    public void CreateFundamental(Fundamental fundamental);
    public IList<Fundamental> GetFundamentals();

    //UPDATE??

    public int DeleteFundamental(Guid id);
    public int DeleteFundamentalValues(Guid id);
    public int DeleteFundamentalValues(string dataProviderName, Guid id);

    public void CreateCountryFundamental(CountryFundamental fundamental);
    public IList<CountryFundamental> GetCountryFundamentals(string dataProviderName);
    public void UpdateCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime, double value);
    public int DeleteCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId);
    public int DeleteCountryFundamentalValue(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime);

    public void CreateInstrumentFundamental(InstrumentFundamental fundamental);
    public IList<InstrumentFundamental> GetInstrumentFundamentals(string dataProviderName);
    public void UpdateInstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime, double value);
    public int DeleteInstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId);
    public int DeleteInstrumentFundamentalValue(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime);

    /// <summary>
    /// Create/update price data from a given DataProvider for a given instrument. Synthetic data that was generated can be set for a given instrument as well in the event that the specific
    /// DataProvider has missing intervals of data.
    /// </summary>
    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume, bool synthetic);
    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, BarData bars);
    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Level1Data bars);
    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime dateTime, bool? synthetic = null);
    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution = null, DateTime? from = null, DateTime? to = null, bool? synthetic = null);
    public DataCache GetInstrumentData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, DateTime from, DateTime to, PriceDataType priceDataType);

  }
}