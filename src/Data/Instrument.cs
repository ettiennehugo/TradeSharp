using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.Common;
using static TradeSharp.Data.IDataStoreService;
using System.Globalization;
using System.Diagnostics.Metrics;

namespace TradeSharp.Data
{
  /// <summary>
  /// Storage instance of an instrument.
  /// </summary>
  public class Instrument : DescriptionObject, IInstrument
  {

    //constants


    //enums


    //types


    //attributes
    protected List<IInstrumentFundamental> m_fundamentals;
    protected List<IExchange> m_otherExchanges;

    //constructors
    public Instrument(IDataStoreService dataStore, IDataManagerService dataManager, string name, string description) : base(dataStore, dataManager, name, description)
    {
      Type = InstrumentType.None;
      Ticker = string.Empty;
      InceptionDate = DateTime.MinValue;
      PrimaryExchange = ExchangeNone.Instance;
      m_fundamentals = new List<IInstrumentFundamental>();
      m_otherExchanges = new List<IExchange>();
      InstrumentGroupIds = new List<Guid>();
      InstrumentGroups = new List<IInstrumentGroup>();
    }

    public Instrument(IDataStoreService dataStore, IDataManagerService dataManager, IExchange exchange, InstrumentType type, string ticker, string name, string description, DateTime inceptionDate) : this(dataStore, dataManager, name, description)
    {
      Type = type;
      Ticker = ticker;
      PrimaryExchange = exchange;
      InceptionDate = inceptionDate;
    }

    public Instrument(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Instrument instrument) : this(dataStore, dataManager, instrument.Name, instrument.Description)
    {
      Id = instrument.Id;
      NameTextId = instrument.NameTextId;
      DescriptionTextId = instrument.DescriptionTextId;
      Name = DataStore.GetText(NameTextId);
      Description = DataStore.GetText(DescriptionTextId);
      Type = instrument.Type;
      Ticker = instrument.Ticker;
      InceptionDate = instrument.InceptionDate;
      InstrumentGroupIds = instrument.InstrumentGroupIds; 
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentType Type { get; }
    public string Ticker { get; set; }
    public IExchange PrimaryExchange { get; set; }
    public IList<IInstrumentFundamental> Fundamentals => m_fundamentals;
    public IList<IExchange> SecondaryExchanges => m_otherExchanges;
    public DateTime InceptionDate { get; set; }
    public IList<Guid> InstrumentGroupIds { get; }
    public IList<IInstrumentGroup> InstrumentGroups { get; }

    //methods
    public void Add(IInstrumentFundamental fundamental) { m_fundamentals.Add(fundamental); }
    public void Remove(IInstrumentFundamental fundamental) { m_fundamentals.Remove(fundamental); }
    
    public void Add(IExchange exchange)
    {
      if (PrimaryExchange.Id != exchange.Id) m_otherExchanges.Add(exchange);
    }

    public void Remove(IExchange exchange)
    {
      if (PrimaryExchange.Id != exchange.Id) m_otherExchanges.Remove(exchange);
    }

    public void Add(IInstrumentGroup instrumentGroup)
    {
      if (!InstrumentGroups.Contains(instrumentGroup))
      {
        InstrumentGroupIds.Add(instrumentGroup.Id);
        InstrumentGroups.Add(instrumentGroup);
      }
    }

    public void Remove(IInstrumentGroup instrumentGroup)
    {
      InstrumentGroupIds.Remove(instrumentGroup.Id);
      InstrumentGroups.Remove(instrumentGroup);
    }
  }


  /// <summary>
  /// Special null object instrument where needed.
  /// </summary>
  public class InstrumentNone : IInstrument
  {

    //constants


    //enums


    //types


    //attributes
    private static InstrumentNone m_instance;

    //constructors
    static InstrumentNone()
    {
      m_instance = new InstrumentNone();
    }

    //finalizers


    //interface implementations


    //properties
    public static InstrumentNone Instance => m_instance;
    public Guid Id => Guid.Empty;
    public IExchange PrimaryExchange => ExchangeNone.Instance;
    public IList<IExchange> SecondaryExchanges => Array.Empty<IExchange>();
    public IList<IInstrumentFundamental> Fundamentals => Array.Empty<IInstrumentFundamental>();
    public string Ticker => string.Empty;
    public InstrumentType Type => InstrumentType.None;
    public DateTime InceptionDate => DateTime.MaxValue;
    public string Name {  get => Common.Resources.InstrumentNoneName; set { /* nothing to do */ } }
    public Guid NameTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public string Description {  get => Common.Resources.InstrumentNoneDescription; set { /* nothing to do */ } }
    public Guid DescriptionTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public IList<IInstrumentGroup> InstrumentGroups => Array.Empty<IInstrumentGroup>();

    //methods
    public string DescriptionInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentNoneDescription", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.InstrumentNoneDescription;
    }

    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentNoneName", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.InstrumentNoneName;
    }

    string IDescription.DescriptionInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentNoneDescription", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.InstrumentNoneDescription;
    }

    string IName.NameInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentNoneName", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.InstrumentNoneName;
    }
  }


}
