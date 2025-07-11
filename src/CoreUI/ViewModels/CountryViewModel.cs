﻿using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of countries with their details using the associated item view model.
  /// </summary>
  public class CountryViewModel : ListViewModel<Country>, ICountryViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public CountryViewModel(ICountryService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      m_dialogService.ShowCreateCountryAsync();
    }

    public override void OnDelete(object? target)
    {
      if (SelectedItem != null)
        m_itemsService.Delete(SelectedItem);
      else
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "No country selected to delete.");
    }

    public override void OnUpdate()
    {
      throw new NotImplementedException("Update not supported for countries.");
    }

    //properties



    //methods


  }
}
