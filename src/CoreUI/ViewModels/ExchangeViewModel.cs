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
  public class ExchangeViewModel : MasterDetailViewModel<ExchangeItemViewModel, Exchange>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExchangeViewModel(IItemsService<Exchange> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) { }

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

    protected override ExchangeItemViewModel ToViewModel(Exchange item)
    {
      return new ExchangeItemViewModel(item, m_navigationService, m_dialogService);
    }

  }
}
