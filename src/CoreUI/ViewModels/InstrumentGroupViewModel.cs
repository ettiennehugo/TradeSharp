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
    public InstrumentGroupViewModel(IInstrumentGroupService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? target) => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Deletable));
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? target) => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      Guid parentId = SelectedNode != null ? SelectedNode.Item.Id : InstrumentGroup.InstrumentGroupRoot;
      InstrumentGroup? newInstrumentGroup = await m_dialogService.ShowCreateInstrumentGroupAsync(parentId);
      if (newInstrumentGroup != null)
      {
        m_itemsService.Add(new InstrumentGroupNodeType((IInstrumentGroupService)m_itemsService, newInstrumentGroup));
        m_itemsService.Refresh(parentId);
      }
    }

    public override async void OnUpdate()
    {
      if (SelectedNode != null)
      {
        var updatedInstrumentGroup = await m_dialogService.ShowUpdateInstrumentGroupAsync(SelectedNode.Item);
        if (updatedInstrumentGroup != null)
        {
          SelectedNode.Item.Update(updatedInstrumentGroup);
          m_itemsService.Update(SelectedNode!);
        }
      }
      else
        await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "Please select a node to update");
    }

    protected Task OnRefreshAsync(Guid parentId)
    {
      return Task.Run(() => m_itemsService.Refresh(parentId));
    }

    public override Task OnCopyAsync(object? target)
    {
      return Task.Run(async () => {
        if (SelectedNode != null)
          m_itemsService.Copy(SelectedNode);
        else
          await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "Please select a node to copy");
      });
    }

    //properties


    //methods


  }
}
