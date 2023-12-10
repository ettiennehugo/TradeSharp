using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Repositories;
using CommunityToolkit.Mvvm.DependencyInjection;

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

    //constructors
    public InstrumentBarDataService() 
    {
      m_repository = Ioc.Default.GetRequiredService<IInstrumentBarDataRepository>();  //need to do this to get a unique transient repository instance associated with this specific service
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      m_selectedItem = null;
      Items = new ObservableCollection<IBarData>();
    }

    //finalizers


    //interface implementations
    public async Task<IBarData> AddAsync(IBarData item)
    {
      var result = await m_repository.AddAsync(item);
      Utilities.SortedInsert(item, Items);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public Task<IBarData> CopyAsync(IBarData item) => throw new NotImplementedException();  //TODO: Need to figure out how this would occur, maybe override method to support copy to different resolutions and PriceTypes.

    public async Task<bool> DeleteAsync(IBarData item)
    {
      bool result = await m_repository.DeleteAsync(item);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }

      Items.Remove(item);

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

    public async Task<IBarData> UpdateAsync(IBarData item)
    {
      IBarData barData = await m_repository.UpdateAsync(item);
      
      //the bar editor does not allow modification of the DateTime and Synthetic settings
      for (int i = 0; i < Items.Count(); i++)
        if (barData.Equals(item))
        {
          Items.RemoveAt(i);
          Items.Insert(i, barData);
          return barData;
        }

      return barData;
    }

    //properties
    public Guid ParentId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } //not supported, instrument bar data requires a complex key

    public string DataProvider { get => m_repository.DataProvider; set => m_repository.DataProvider = value; }
    public Instrument? Instrument { get => m_repository.Instrument; set => m_repository.Instrument = value; }
    public Resolution Resolution { get => m_repository.Resolution; set => m_repository.Resolution = value; }
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
