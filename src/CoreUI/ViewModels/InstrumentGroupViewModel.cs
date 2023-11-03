using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of instrument groups defined and the instruments contained by them.
  /// </summary>
  public class InstrumentGroupViewModel : ListViewModel<InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes
    public RelayCommand ImportCommand { get; internal set; }
    public RelayCommand ExportCommand { get; internal set; }

    //constructors
    public InstrumentGroupViewModel(IItemsService<InstrumentGroup> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null && SelectedNode.InstrumentGroup != null && SelectedNode.InstrumentGroup.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedNode != null && SelectedNode.InstrumentGroup != null && SelectedNode.InstrumentGroup.HasAttribute(Attributes.Deletable));
      ImportCommand = new RelayCommand(OnImport);
      ExportCommand = new RelayCommand(OnExport);
    }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      InstrumentGroup? newInstrumentGroup = await m_dialogService.ShowCreateInstrumentGroupAsync(SelectedItem != null ? SelectedItem.Id : InstrumentGroup.InstrumentGroupRoot);
      if (newInstrumentGroup != null)
      {
        await m_itemsService.AddAsync(newInstrumentGroup);
        Items.Add(newInstrumentGroup);
        SelectedItem = newInstrumentGroup;
        await OnRefreshAsync();
      }
    }

    public async override void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateInstrumentGroupAsync(SelectedItem);
        if (updatedSession != null)
        {
          await m_itemsService.UpdateAsync(updatedSession);
          await OnRefreshAsync();
        }
      }
    }

    public async void OnImport()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentGroupsAsync();
      
      if (importSettings != null)
      {
        int importCount = await m_itemsService.ImportAsync(importSettings.Filename, importSettings.ImportReplaceBehavior);
        await m_dialogService.ShowStatusMessageAsync(importCount == 0 ? IDialogService.StatusMessageSeverity.Warning : IDialogService.StatusMessageSeverity.Success, "", $"Imported {importCount} instrument groups");
      }
    }

    public async void OnExport()
    {
      string? filename = await m_dialogService.ShowExportInstrumentGroupsAsync();

      if (filename != null)
      {
        int exportCount = await m_itemsService.ExportAsync(filename);
        await m_dialogService.ShowStatusMessageAsync(exportCount == 0 ? IDialogService.StatusMessageSeverity.Warning : IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} instrument groups");
      }
    }

    //properties
    public ObservableCollection<InstrumentGroupServiceNode> Nodes => ((InstrumentGroupService)m_itemsService).Nodes;

    /// <summary>
    /// Selected node in the tree view model.
    /// </summary>
    public virtual InstrumentGroupServiceNode? SelectedNode
    {
      get => ((InstrumentGroupService)m_itemsService).SelectedNode;
      set
      {
        InstrumentGroupService igs = (InstrumentGroupService)m_itemsService;
        if (!EqualityComparer<InstrumentGroupServiceNode>.Default.Equals(igs.SelectedNode, value))
        {
          igs.SelectedNode = value;
          SelectedItem = igs.SelectedNode != null ? igs.SelectedNode.InstrumentGroup : null;
          OnPropertyChanged();
          AddCommand.NotifyCanExecuteChanged();
          UpdateCommand.NotifyCanExecuteChanged();
          DeleteCommand.NotifyCanExecuteChanged();
          RefreshCommand.NotifyCanExecuteChanged();
          RefreshCommandAsync.NotifyCanExecuteChanged();
        }
      }
    }



    //methods


  }
}
