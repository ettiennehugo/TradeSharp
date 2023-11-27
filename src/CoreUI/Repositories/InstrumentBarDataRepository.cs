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
      DataProvider = "";
      InstrumentId = Guid.Empty;
      Ticker = "";
      Start = DateTime.MinValue;
      End = DateTime.MaxValue;
      Resolution = Resolution.Day;
      PriceDataType = PriceDataType.Both; //by default return all available data
      m_dataStore = dataStoreService;
    }

    //finalizers


    //interface implementations
    public Task<IBarData?> GetItemAsync(DateTime id)
    {
      throwIfNotKeyed();
      return Task.FromResult(m_dataStore.GetBarData(DataProvider, InstrumentId, Ticker, Resolution, id, PriceDataType));
    }

    public Task<IEnumerable<IBarData>> GetItemsAsync()
    {
      throwIfNotKeyed();
      return Task.FromResult<IEnumerable<IBarData>>(m_dataStore.GetBarData(DataProvider, InstrumentId, Ticker, Resolution, Start, End, PriceDataType));
    }

    public Task<IBarData> AddAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_dataStore.UpdateData(DataProvider, InstrumentId, Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume, item.Synthetic); return item; } );
    }

    public Task<IBarData> UpdateAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.Run(() => { m_dataStore.UpdateData(DataProvider, InstrumentId, Ticker, Resolution, item.DateTime, item.Open, item.High, item.Low, item.Close, item.Volume, item.Synthetic); return item; });
    }

    public Task<bool> DeleteAsync(IBarData item)
    {
      throwIfNotKeyed();
      return Task.FromResult(m_dataStore.DeleteData(DataProvider, Ticker, Resolution, item.DateTime, item.Synthetic) != 0);
    }

    //properties
    public string DataProvider { get; set; }
    public Guid InstrumentId { get; set; }
    public string Ticker { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Resolution Resolution { get; set; }
    public PriceDataType PriceDataType { get; set; }

    //methods
    private void throwIfNotKeyed()
    {
      if (DataProvider == "") throw new KeyNotFoundException("DataProvider must have a value.");
      if (InstrumentId == Guid.Empty) throw new KeyNotFoundException("InstrumentId must have a value.");
      if (Ticker == "") throw new KeyNotFoundException("Ticker must have a value.");
    }
  }
}
