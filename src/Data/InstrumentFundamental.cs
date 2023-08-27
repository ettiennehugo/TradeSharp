using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Fundamental factor associated with an instrument.
  /// </summary>
  public class InstrumentFundamental : IInstrumentFundamental
  {
    //constants


    //enums


    //types


    //attributes
    protected SortedDictionary<DateTime, Decimal> m_values;

    //constructors
    public InstrumentFundamental(IFundamental fundamental, IInstrument instrument)
    {
      if (fundamental.Category != FundamentalCategory.Instrument) throw new ArgumentException("Fundamental is not instrument specific");
      AssociationId = Guid.Empty;
      FundamentalId = fundamental.Id;
      Fundamental = fundamental;
      InstrumentId = instrument.Id;
      Instrument = instrument;
      m_values = new SortedDictionary<DateTime, decimal>();
    }

    public InstrumentFundamental(IDataStoreService.InstrumentFundamental fundamental) : this(FundamentalNone.Instance, InstrumentNone.Instance)
    {
      AssociationId = fundamental.AssociationId;
      FundamentalId = fundamental.FundamentalId;
      InstrumentId = fundamental.InstrumentId;
    }

    //finalizers


    //interface implementations


    //properties
    public Guid AssociationId { get; set; }
    public Guid FundamentalId { get; }
    public Guid InstrumentId { get; }
    public IFundamental Fundamental { get; set; }
    public IInstrument Instrument { get; set; }
    public KeyValuePair<DateTime, decimal>? Latest { get { return m_values.Count > 0 ? m_values.ElementAt(m_values.Count - 1) : null; } }
    public IDictionary<DateTime, Decimal> Values => m_values;

    //methods
    internal void Add(DateTime dateTime, Decimal value)
    {
      if (m_values.ContainsKey(dateTime)) 
        m_values[dateTime] = value;
      else 
        m_values.Add(dateTime, value);
    }
    
    internal void Clear() { m_values.Clear(); }

  }
}
