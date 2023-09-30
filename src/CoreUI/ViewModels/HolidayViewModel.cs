using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.Input;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of holidays with their details using the associated item view model.
  /// </summary>
  public partial class HolidayViewModel : MasterDetailViewModel<HolidayItemViewModel, Holiday>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public HolidayViewModel(IItemsService<Holiday> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) 
    {
      AddCommand = new RelayCommand(OnAdd, () => ParentId != Guid.Empty);
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public async override void OnAdd()
    {
      Holiday? newHoliday = await m_dialogService.ShowCreateHolidayAsync(m_itemsService.ParentId);
      if (newHoliday != null)
      {
        if (m_itemsService.Items.Contains(newHoliday))
          await m_dialogService.ShowMessageAsync("The holiday you are trying to add already exists in the database.");
        else
        {
          await m_itemsService.AddAsync(newHoliday);
          Items.Add(newHoliday);
          SelectedItem = newHoliday;
        }
      }
    }

    public async override void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedHoliday = await m_dialogService.ShowUpdateHolidayAsync(SelectedItem);
        if (updatedHoliday != null)
        {
          await m_itemsService.UpdateAsync(updatedHoliday);
          await OnRefreshAsync();
        }
      }
    }

    public override void OnDelete()
    {
      if (SelectedItem != null)
      {
        var item = SelectedItem;
        Items.Remove(SelectedItem);
        m_itemsService.DeleteAsync(item);
        SelectedItem = Items.FirstOrDefault();
      }
    }

    protected override HolidayItemViewModel ToViewModel(Holiday item)
    {
      return new HolidayItemViewModel(item, m_navigationService, m_dialogService);
    }
  }
}
