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
    private IDataStoreService m_dataStore;

    //constructors
    public InstrumentBarDataRepository(IDataStoreService dataStoreService) 
    {
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      m_dataStore = dataStoreService;
    }

    //finalizers


    //interface implementations
    public Task<IBarData?> GetItemAsync(DateTime id)
    {
      throwIfNotKeyed();
      return Task.FromResult(m_dataStore.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, id, PriceDataType.All)); //all data is always returned and filtered down in service/view model/UI
    }

    public Task<IEnumerable<IBarData>> GetItemsAsync()
    {
      throwIfNotKeyed();
      return Task.FromResult<IEnumerable<IBarData>>(m_dataStore.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, DateTime.MinValue, DateTime.MaxValue, PriceDataType.All)); //all data is always returned and filtered down in service/view model/UI
    }

    public Task<IEnumerable<IBarData>> GetItemsAsync(DateTime start, DateTime end)
    {
      throwIfNotKeyed();
      return Task.FromResult<IEnumerable<IBarData>>(m_dataStore.GetBarData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, start, end, PriceDataType.All)); //all data is always returned and filtered down in service/view model/UI
    }

    public Task<IBarData> AddAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_dataStore.UpdateData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume, item.Synthetic); return item; } );
    }

    public Task<IBarData> UpdateAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_dataStore.UpdateData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume, item.Synthetic); return item; });
    }

    public Task<long> UpdateAsync(IList<IBarData> items)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_dataStore.UpdateData(DataProvider, Instrument!.Id, Instrument.Ticker, Resolution, items); return (long)items.Count; });
    }

    public Task<bool> DeleteAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.FromResult(m_dataStore.DeleteData(DataProvider, Instrument!.Ticker, Resolution, item.DateTime, item.Synthetic) != 0);
    }

    //properties
    public string DataProvider { get; set; }
    public Instrument? Instrument { get; set; }
    public Resolution Resolution { get; set; }

    //methods
    private void throwIfNotKeyed()
    {
      if (DataProvider == string.Empty) throw new KeyNotFoundException("DataProvider must have a value.");
      if (Instrument == null) throw new KeyNotFoundException("Instrument must have a value.");
    }
  }
}
