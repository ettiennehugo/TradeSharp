using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of holidays associated with a country of exchange specified as the parent Id of the base class.
  /// </summary>
  public partial class HolidayViewModel : ListViewModel<Holiday>, IHolidayViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public HolidayViewModel(IHolidayService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      AddCommand = new RelayCommand(OnAdd, () => ParentId != Guid.Empty);
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public override async void OnAdd()
    {
      Holiday? newHoliday = await m_dialogService.ShowCreateHolidayAsync(m_itemsService.ParentId);
      if (newHoliday != null)
      {
        if (m_itemsService.Items.Contains(newHoliday))
          await m_dialogService.ShowPopupMessageAsync("The holiday you are trying to add already exists in the database.");
        else
        {
          m_itemsService.Add(newHoliday);
          Items.Add(newHoliday);
          SelectedItem = newHoliday;
        }
      }
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedHoliday = await m_dialogService.ShowUpdateHolidayAsync(SelectedItem);
        if (updatedHoliday != null)
          m_itemsService.Update(updatedHoliday);
        //TBD: Will need a refresh of the list item here to make sure the UI updates.
      }
    }
  }
}
