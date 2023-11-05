using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of exchanges with their details.
  /// </summary>
  public class ExchangeViewModel : ListViewModel<Exchange>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExchangeViewModel(IListItemsService<Exchange> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public async override void OnAdd()
    {
      Exchange? exchange = await m_dialogService.ShowCreateExchangeAsync();
      if (exchange != null)
      {
        await m_itemsService.AddAsync(exchange);
        SelectedItem = exchange;
        Items.Add(exchange);
        await OnRefreshAsync();

      }
    }

    public async override void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedExchange = await m_dialogService.ShowUpdateExchangeAsync(SelectedItem);
        if (updatedExchange != null)
        {
          await m_itemsService.UpdateAsync(updatedExchange);
          await OnRefreshAsync();
        }
      }
    }
  }
}
