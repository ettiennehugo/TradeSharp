using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

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
    public InstrumentViewModel(IInstrumentService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) { }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      Instrument? newInstrument = await m_dialogService.ShowCreateInstrumentAsync();
      if (newInstrument != null)
        m_itemsService.Add(newInstrument);
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateInstrumentAsync(SelectedItem);
        if (updatedSession != null)
          m_itemsService.Update(updatedSession);
      }
    }

    public override async Task OnImportAsync()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentsAsync();
      if (importSettings != null) _ = Task.Run(() => m_itemsService.Import(importSettings));
    }

    public override async Task OnExportAsync()
    {
      string? filename = await m_dialogService.ShowExportInstrumentsAsync();
      if (filename != null) _ = Task.Run(() => m_itemsService.Export(filename));
    }

    public override Task OnRefreshAsync()
    {
      return Task.Run(() => m_itemsService.Refresh());
    }

    //properties


    //methods


  }
}
