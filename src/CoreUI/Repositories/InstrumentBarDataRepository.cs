using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on instrument data running parallel to the UI thread.
  /// </summary>
  public partial class InstrumentBarDataRepository : ObservableObject, IInstrumentBarDataRepository
  {
    //constants


    //enums


    //types


    //attributes
    private IDatabase m_database;
    private long m_index;   //current index for paged reading of bar data
    private long m_count;   //number of bar data items on the database

    //constructors
    public InstrumentBarDataRepository(IDatabase database) 
    {
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      m_database = database;
      m_index = 0;
      m_count = 0;
    }

    //finalizers


    //interface implementations
    public int GetCount()
    {
      throwIfNotKeyed();
      return m_database.GetDataCount(DataProvider, Instrument!.Ticker, Resolution);
    }

    public int GetCount(DateTime from, DateTime to)
    {
      throwIfNotKeyed();
      return m_database.GetDataCount(DataProvider, Instrument!.Ticker, Resolution, from, to);
    }

    public IBarData? GetItem(DateTime id)
    {
      throwIfNotKeyed();
      return m_database.GetBarData(DataProvider, Instrument!.Ticker, Resolution, id);
    }

    public IList<IBarData> GetItems()
    {
      throwIfNotKeyed();
      return m_database.GetBarData(DataProvider, Instrument!.Ticker, Resolution, DateTime.MinValue, DateTime.MaxValue);
    }

    public IList<IBarData> GetItems(DateTime start, DateTime end)
    {
      throwIfNotKeyed();
      return m_database.GetBarData(DataProvider, Instrument!.Ticker, Resolution, start, end);
    }

    public IList<IBarData> GetItems(int index, int count)
    {
      throwIfNotKeyed();
      return m_database.GetBarData(DataProvider, Instrument!.Ticker, Resolution, index, count);
    }

    public IList<IBarData> GetItems(DateTime start, DateTime end, int index, int count)
    {
      throwIfNotKeyed();
      return m_database.GetBarData(DataProvider, Instrument!.Ticker, Resolution, start, end, index, count);
    }

    public bool Add(IBarData item)
    {
      throwIfNotKeyed();
      m_database.UpdateData(DataProvider, Instrument!.Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume);
      return true; 
    }

    public bool Update(IBarData item)
    {
      throwIfNotKeyed();
      m_database.UpdateData(DataProvider, Instrument!.Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume);
      return true;
    }

    public int Update(IList<IBarData> items)
    {
      throwIfNotKeyed();
      m_database.UpdateData(DataProvider, Instrument!.Ticker, Resolution, items);
      return items.Count;
    }

    public int Delete()
    {
      throwIfNotKeyed();
      return m_database.DeleteData(DataProvider, Instrument!.Ticker, Resolution);
    }

    public int Delete(IList<IBarData> items)
    {
      throwIfNotKeyed();
      int result = 0;
      foreach (IBarData item in items)
      {
        m_database.DeleteData(DataProvider, Instrument!.Ticker, Resolution, item.DateTime);
        result++;
      }
      return result;
    }

    public bool Delete(IBarData item)
    {
      throwIfNotKeyed();
      return m_database.DeleteData(DataProvider, Instrument!.Ticker, Resolution, item.DateTime) != 0;
    }

    public int Delete(DateTime from, DateTime to)
    {
      throwIfNotKeyed();
      return m_database.DeleteData(DataProvider, Instrument!.Ticker, Resolution, from, to);
    }

    //properties
    public string DataProvider { get; set; }
    public Instrument? Instrument { get; set; }
    public Resolution Resolution { get; set; }
    public bool HasMoreItems => DataProvider != string.Empty && Instrument != null && m_index < m_count;

    //methods
    private void throwIfNotKeyed()
    {
      if (DataProvider == string.Empty) throw new KeyNotFoundException("DataProvider must have a value.");
      if (Instrument == null) throw new KeyNotFoundException("Instrument must have a value.");
    }
  }
}
