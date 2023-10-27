using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

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
      ImportCommand = new RelayCommand(OnImport);
      ExportCommand = new RelayCommand(OnExport);
    }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      InstrumentGroup? newInstrumentGroup = await m_dialogService.ShowCreateInstrumentGroupAsync();
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

    public void OnImport()
    {
      throw new NotImplementedException();
    }

    public void OnExport()
    {
      throw new NotImplementedException();
    }

    //properties


    //methods


  }
}
