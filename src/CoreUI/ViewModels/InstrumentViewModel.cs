using CommunityToolkit.Common.Collections;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for list of instruments.
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
    public async override void OnAdd()
    {
      Instrument? newInstrument = await m_dialogService.ShowCreateInstrumentAsync();
      if (newInstrument != null)
      {
        await m_itemsService.AddAsync(newInstrument);
        Items.Add(newInstrument);
        SelectedItem = newInstrument;
        await OnRefreshAsync();
      }
    }

    public async override void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateInstrumentAsync(SelectedItem);
        if (updatedSession != null)
        {
          await m_itemsService.UpdateAsync(updatedSession);
          await OnRefreshAsync();
        }
      }
    }

    public override async void OnImport()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentsAsync();

      if (importSettings != null)
      {
        ImportResult importResult = await m_itemsService.ImportAsync(importSettings);
        await m_dialogService.ShowStatusMessageAsync(importResult.Severity, "", importResult.StatusMessage);
        await OnRefreshAsync();
      }
    }

    public override async void OnExport()
    {
      string? filename = await m_dialogService.ShowExportInstrumentsAsync();

      if (filename != null)
      {
        ExportResult exportResult = await m_itemsService.ExportAsync(filename);
        await m_dialogService.ShowStatusMessageAsync(exportResult.Severity, "", exportResult.StatusMessage);
      }
    }

    //properties


    //methods


  }
}
