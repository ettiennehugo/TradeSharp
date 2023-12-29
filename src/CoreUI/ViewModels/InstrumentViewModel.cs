using CommunityToolkit.Common.Collections;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for list of instruments, it supports incremental loading of the objects from the service.
  /// </summary>
  public class InstrumentViewModel : ListViewModel<Instrument>
  {
    //constants


    //enums


    //types


    //attributes

    //constructors
    public InstrumentViewModel(IInstrumentService itemsService, INavigationService navigationService, IDialogService dialogService): base(itemsService, navigationService, dialogService) { }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      Instrument? newInstrument = await m_dialogService.ShowCreateInstrumentAsync();
      if (newInstrument != null)
      {
        m_itemsService.Add(newInstrument);
        Items.Add(newInstrument);
        SelectedItem = newInstrument;
        await OnRefreshAsync();   //TODO: This will not work for large collections.
      }
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateInstrumentAsync(SelectedItem);
        if (updatedSession != null)
        {
          m_itemsService.Update(updatedSession);
          await OnRefreshAsync(); //TODO: This will not work for large collections.
        }
      }
    }

    public override Task OnImportAsync()
    {
      return Task.Run(async () => {
        ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentsAsync();

        if (importSettings != null)
        {
          ImportResult importResult = m_itemsService.Import(importSettings);
          await m_dialogService.ShowStatusMessageAsync(importResult.Severity, "", importResult.StatusMessage);
          await OnRefreshAsync();
        }
      });
    }

    public override Task OnExportAsync()
    {
      return Task.Run(async () => {
        string? filename = await m_dialogService.ShowExportInstrumentsAsync();

        if (filename != null)
        {
          ExportResult exportResult = m_itemsService.Export(filename);
          await m_dialogService.ShowStatusMessageAsync(exportResult.Severity, "", exportResult.StatusMessage);
        }
      });
    }

    //properties
    public override ObservableCollection<Instrument> Items { get => m_itemsService.Items; set => m_itemsService.Items = value; }

    //methods


  }
}
