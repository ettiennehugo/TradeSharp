using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public RelayCommand ImportCommand { get; internal set; }
    public RelayCommand ExportCommand { get; internal set; }

    //constructors
    public InstrumentViewModel(IItemsService<Instrument> itemsService, INavigationService navigationService, IDialogService dialogService): base(itemsService, navigationService, dialogService)
    {
      ImportCommand = new RelayCommand(OnImport);
      ExportCommand = new RelayCommand(OnExport);
    }

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
