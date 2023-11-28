using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Repositories;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for instrument bar data objects.
  /// </summary>
  public partial class InstrumentBarDataService : ObservableObject, IInstrumentBarDataService
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentBarDataRepository m_repository;
    private IBarData? m_selectedItem;
    private Instrument? m_instrument;

    //constructors
    public InstrumentBarDataService(IInstrumentBarDataRepository repository) 
    {
      m_repository = repository;
      DataProvider = "";
      Instrument = null;
      Ticker = "";
      Start = DateTime.MinValue;
      End = DateTime.MaxValue;
      Resolution = Resolution;
      PriceDataType = PriceDataType.Both;
      m_selectedItem = null;
      Items = new ObservableCollection<IBarData>();
    }

    //finalizers


    //interface implementations
    public async Task<IBarData> AddAsync(IBarData item)
    {
      var result = await m_repository.AddAsync(item);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public Task<IBarData> CopyAsync(IBarData item) => throw new NotImplementedException();

    public async Task<bool> DeleteAsync(IBarData item)
    {
      bool result = await m_repository.DeleteAsync(item);

      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }

      return result;
    }

    public Task<long> ExportAsync(string filename)
    {
      throw new NotImplementedException();    //TODO: Write code to export data async.
    }

    public Task<ImportReplaceResult> ImportAsync(ImportSettings importSettings)
    {
      throw new NotImplementedException();  //TODO: Write code to import data async.
    }

    public async Task RefreshAsync()
    {
      var result = await m_repository.GetItemsAsync();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<IBarData> UpdateAsync(IBarData item)
    {
      return m_repository.UpdateAsync(item);
    }

    //properties
    public Guid ParentId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } //not supported, instrument bar data requires a complex key

    public string DataProvider { get => m_repository.DataProvider; set => m_repository.DataProvider = value; }
    
    public Instrument? Instrument 
    {
      get => m_instrument;
      set
      {
        m_instrument = value;
        m_repository.InstrumentId = m_instrument != null ? m_instrument.Id : Guid.Empty;
      }
    }
    
    public string Ticker { get => m_repository.Ticker; set => m_repository.Ticker = value; }
    public DateTime Start { get => m_repository.Start; set => m_repository.Start = value; }
    public DateTime End { get => m_repository.End; set => m_repository.End = value; }
    public Resolution Resolution { get => m_repository.Resolution; set => m_repository.Resolution = value; }
    public PriceDataType PriceDataType { get => m_repository.PriceDataType; set => m_repository.PriceDataType = value; }

    public event EventHandler<IBarData?>? SelectedItemChanged;
    public IBarData? SelectedItem 
    { 
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public ObservableCollection<IBarData> Items { get; set; }

    //methods


  }
}
