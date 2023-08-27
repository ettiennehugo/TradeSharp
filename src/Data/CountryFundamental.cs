using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TradeSharp.Data;

namespace TradeSharp.Data
{
  /// <summary>
  /// Fundamental factor associated with a country.
  /// </summary>
  public class CountryFundamental : ICountryFundamental
  {
    //constants


    //enums


    //types


    //attributes
    protected SortedDictionary<DateTime, decimal> m_values;

    //constructors
    public CountryFundamental(IFundamental fundamental, ICountry country)
    {
      if (fundamental.Category != FundamentalCategory.Country) throw new ArgumentException("Fundamental is not country specific");
      AssociationId = Guid.Empty;
      FundamentalId = fundamental.Id;
      Fundamental = fundamental;
      CountryId = country.Id;
      Country = country;
      m_values = new SortedDictionary<DateTime, decimal>();
    }

    public CountryFundamental(IDataStoreService.CountryFundamental fundamental) : this(FundamentalNone.Instance, CountryInternational.Instance)
    {
      AssociationId = fundamental.AssociationId;
      FundamentalId = fundamental.FundamentalId;
      CountryId = fundamental.CountryId;
    }

    //finalizers


    //interface implementations


    //properties
    public Guid AssociationId { get; set; }
    public Guid FundamentalId { get; }
    public Guid CountryId { get; }
    public IFundamental Fundamental { get; set; }
    public ICountry Country { get; set; }
    public KeyValuePair<DateTime, decimal>? Latest { get { return m_values.Count > 0 ?  m_values.ElementAt(m_values.Count - 1) : null; } }
    public IDictionary<DateTime, decimal> Values => m_values;

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
