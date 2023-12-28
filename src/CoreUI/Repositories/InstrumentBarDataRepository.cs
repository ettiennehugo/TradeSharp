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
    public Task<IBarData?> GetItemAsync(DateTime id)
    {
      throwIfNotKeyed();
      return Task.FromResult(m_database.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, id, PriceDataType.All)); //all data is always returned and filtered down in service/view model/UI
    }

    public Task<IEnumerable<IBarData>> GetItemsAsync()
    {
      throwIfNotKeyed();
      return Task.FromResult<IEnumerable<IBarData>>(m_database.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, DateTime.MinValue, DateTime.MaxValue, PriceDataType.All)); //all data is always returned and filtered down in service/view model/UI
    }

    public Task<IEnumerable<IBarData>> GetItemsAsync(DateTime start, DateTime end)
    {
      throwIfNotKeyed();
      return Task.FromResult<IEnumerable<IBarData>>(m_database.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, start, end, PriceDataType.All)); //all data is always returned and filtered down in service/view model/UI
    }

    public Task<IEnumerable<IBarData>> GetItemsAsync(int index, int count)
    {
      throwIfNotKeyed();
      return Task.FromResult<IEnumerable<IBarData>>(m_database.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, index, count, PriceDataType.All));
    }

    public Task<IBarData> AddAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_database.UpdateData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume, item.Synthetic); return item; } ); //TODO: Update count.
    }

    public Task<IBarData> UpdateAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_database.UpdateData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume, item.Synthetic); return item; }); //TODO: Update count.
    }

    public Task<long> UpdateAsync(IList<IBarData> items)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_database.UpdateData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, items); return (long)items.Count; }); //TODO: Update count.
    }

    public Task<bool> DeleteAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.FromResult(m_database.DeleteData(DataProvider, Instrument!.Ticker, Resolution, item.DateTime, item.Synthetic) != 0); //TODO: Update count.
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
