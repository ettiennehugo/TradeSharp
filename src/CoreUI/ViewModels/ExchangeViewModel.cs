using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of exchanges with their details.
  /// </summary>
  public class ExchangeViewModel : ListViewModel<Exchange>, IExchangeViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExchangeViewModel(IExchangeService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
      //DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
      GlobalExchange = GetItem(Exchange.InternationalId)!;
    }

    //finalizers


    //interface implementations


    //properties
    public Exchange GlobalExchange { get; }

    //methods
    public override async void OnAdd()
    {
      Exchange? exchange = await m_dialogService.ShowCreateExchangeAsync();
      if (exchange != null)
      {
        m_itemsService.Add(exchange);
        SelectedItem = exchange;
      }
    }

    public override void OnDelete(object? target)
    {
      if (SelectedItem != null)
        m_itemsService.Delete(SelectedItem);
      else
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "No exchange selected to delete.");
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedExchange = await m_dialogService.ShowUpdateExchangeAsync(SelectedItem);
        if (updatedExchange != null)
        {
          m_itemsService.Update(updatedExchange);
          Exchange? exchange = GetItem(updatedExchange.Id);
          if (exchange != null)
          {
            exchange.Update(updatedExchange);
          }
        }
      }
    }

    public Exchange? GetItem(Guid id)
    {
      return Items.FirstOrDefault(x => x.Id == id);
    }
  }
}
