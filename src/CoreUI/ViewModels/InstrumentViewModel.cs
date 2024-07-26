using System.Collections.ObjectModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.Input;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for list of instruments, it supports incremental loading of the objects from the service.
  /// </summary>
  public class InstrumentViewModel : ListViewModel<Instrument>, IInstrumentViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public InstrumentViewModel(IInstrumentService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) 
    {
      AddStockCommand = new RelayCommand(OnAddStock);
    }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      Instrument? newInstrument = await m_dialogService.ShowCreateInstrumentAsync(InstrumentType.None);
      if (newInstrument != null)
        m_itemsService.Add(newInstrument);
    }

    public virtual async void OnAddStock()
    {
      Instrument? newInstrument = await m_dialogService.ShowCreateInstrumentAsync(InstrumentType.Stock);
      if (newInstrument != null)
        m_itemsService.Add(newInstrument);
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
        await m_dialogService.ShowUpdateInstrumentAsync(SelectedItem);
    }

    public override async Task OnImportAsync()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentsAsync();
      if (importSettings != null) _ = Task.Run(() => m_itemsService.Import(importSettings));
    }

    public override async Task OnExportAsync()
    {
      ExportSettings? exportSettings = await m_dialogService.ShowExportInstrumentsAsync();
      if (exportSettings != null) _ = Task.Run(() => m_itemsService.Export(exportSettings));
    }

    public override Task OnRefreshAsync()
    {
      return Task.Run(() => m_itemsService.Refresh());
    }

    //properties    
    public RelayCommand AddStockCommand { get; set; }

    //methods


  }
}
