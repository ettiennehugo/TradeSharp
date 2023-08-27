using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for instrument groupings.
  /// </summary>
  public class InstrumentGroup : DescriptionObject, IInstrumentGroup
  {

    //constants


    //enums


    //types


    //attributes
    protected Dictionary<Guid, IInstrumentGroup> m_children;
    protected Dictionary<Guid, IInstrument> m_instruments;

    //constructors
    public InstrumentGroup(IDataStoreService dataStore, IDataManagerService dataManager, string name, string description, IInstrumentGroup? parent = null) : base(dataStore, dataManager, name, description) 
    {
      Parent = parent ?? InstrumentGroupRoot.Instance;
      m_children = new Dictionary<Guid, IInstrumentGroup>();
      m_instruments = new Dictionary<Guid, IInstrument>();
    }

    public InstrumentGroup(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.InstrumentGroup instrumentGroup) : this(dataStore, dataManager, instrumentGroup.Name, instrumentGroup.Description, InstrumentGroupRoot.Instance)
    {
      Id = instrumentGroup.Id;
      NameTextId = instrumentGroup.NameTextId;
      DescriptionTextId = instrumentGroup.DescriptionTextId;
      Name = DataStore.GetText(NameTextId);
      Description = DataStore.GetText(DescriptionTextId);
    }

    //finalizers


    //interface implementations


    //properties
    public IInstrumentGroup Parent { get; set; }
    public IList<IInstrumentGroup> Children { get => m_children.Values.ToList(); }
    public virtual IList<IInstrument> Instruments { get => m_instruments.Values.ToList(); }

    //methods
    internal void ClearChildren() { m_children.Clear(); }
    internal void Add(IInstrumentGroup instrumentGroup) { m_children.TryAdd(instrumentGroup.Id, instrumentGroup); }
    internal void Remove(IInstrumentGroup instrumentGroup) { m_children.Remove(instrumentGroup.Id); }

    internal void ClearInstruments() { m_instruments.Clear(); }
    virtual internal void Add(IInstrument instrument) { m_instruments.TryAdd(instrument.Id, instrument); }
    virtual internal void Remove(IInstrument instrument) { m_instruments.Remove(instrument.Id); }
  }

  /// <summary>
  /// Root instrument group.
  /// </summary>
  public sealed class InstrumentGroupRoot : IInstrumentGroup
  {
    //constants


    //enums


    //types


    //attributes
    static private InstrumentGroupRoot m_instance;

    //constructors
    static InstrumentGroupRoot() { m_instance = new InstrumentGroupRoot(); }

    //finalizers


    //interface implementations
    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentGroupRootName", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.InstrumentGroupRootName;
    }

    public string DescriptionInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentGroupRootDescription", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.InstrumentGroupRootDescription;
    }

    string IDescription.DescriptionInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentGroupRootDescription", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.InstrumentGroupRootDescription;
    }

    string IName.NameInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("InstrumentGroupRootName", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.InstrumentGroupRootName;
    }

    //properties
    public static InstrumentGroupRoot Instance { get => m_instance; }
    public Guid Id { get => Guid.Empty; }
    public IInstrumentGroup Parent { get => this; set { /* nothing to do */ } }   //root always return to itself
    public IList<IInstrumentGroup> Children { get => Array.Empty<IInstrumentGroup>(); }
    public IList<IInstrument> Instruments { get => Array.Empty<IInstrument>(); }
    public string Name { get => TradeSharp.Common.Resources.InstrumentGroupRootName; set { /* nothing to do */ } }
    Guid IName.NameTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public string Description { get => TradeSharp.Common.Resources.InstrumentGroupRootDescription; set { /* nothing to do */ } }
    Guid IDescription.DescriptionTextId { get => Guid.Empty; set { /* nothing to do */ } }

    //methods

  }
}
