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
  public class InstrumentGroupViewModel : TreeViewModel<Guid, InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public InstrumentGroupViewModel(ITreeItemsService<Guid, InstrumentGroup> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand(OnDelete, () => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      Guid parentId = SelectedNode != null ? SelectedNode.Item.Id : InstrumentGroup.InstrumentGroupRoot;
      InstrumentGroup? newInstrumentGroup = await m_dialogService.ShowCreateInstrumentGroupAsync(parentId);
      if (newInstrumentGroup != null)
        await m_itemsService.AddAsync(new InstrumentGroupNodeType(m_itemsService, newInstrumentGroup));
    }

    public async override void OnUpdate()
    {
      if (SelectedNode != null)
      {
        var updatedInstrumentGroup = await m_dialogService.ShowUpdateInstrumentGroupAsync(SelectedNode.Item);
        if (updatedInstrumentGroup != null)
        {
          SelectedNode.Item = updatedInstrumentGroup;
          SelectedNode = await m_itemsService.UpdateAsync(SelectedNode!);
        }
      }
      else
        await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "Please select a node to update");
    }

    protected async Task OnRefreshAsync(Guid parentId)
    {
      StartInProgress();
      await m_itemsService.RefreshAsync(parentId);
    }

    public async override void OnCopy()
    {
      if (SelectedNode != null)
        SelectedNode = await m_itemsService.CopyAsync(SelectedNode);
      else
        await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "Please select a node to copy");
    }

    //properties


    //methods


  }
}
