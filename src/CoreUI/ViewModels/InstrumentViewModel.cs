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
    private IInstrumentService m_instrumentService;

    //constructors
    public InstrumentViewModel(IInstrumentService itemsService, INavigationService navigationService, IDialogService dialogService): base(itemsService, navigationService, dialogService) 
    {
      m_instrumentService = itemsService;
    }

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

    public override Task OnImportAsync()
    {
      return Task.Run(async () => {
        ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentsAsync();

        if (importSettings != null)
        {
          ImportResult importResult = m_itemsService.Import(importSettings);
          await m_dialogService.ShowStatusMessageAsync(importResult.Severity, "", importResult.StatusMessage);
          m_itemsService.Refresh();
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

    public override Task OnRefreshAsync()
    {
      return Task.Run(m_itemsService.Refresh);
    }

    public Task<IList<Instrument>> NextPage()
    {
      return Task.FromResult(m_instrumentService.Next());
    }

    public Task<IList<Instrument>> PeekPage()
    {
      return Task.FromResult(m_instrumentService.Peek());
    }

    //properties
    public override IList<Instrument> Items { get => m_itemsService.Items; set => m_itemsService.Items = value; }
    public int Count  { get => m_instrumentService.Count; }
    public int OffsetIndex { get => m_instrumentService.OffsetIndex; set => m_instrumentService.OffsetIndex = value; }
    public int OffsetCount { get => m_instrumentService.OffsetCount; set => m_instrumentService.OffsetCount = value; }
    public bool HasMoreItems { get => m_instrumentService.HasMoreItems; }
    public IDictionary<string, object> Filter { get => m_instrumentService.Filter; set => m_instrumentService.Filter = value; }

    //methods


  }
}
