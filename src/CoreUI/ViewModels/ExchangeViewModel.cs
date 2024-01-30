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
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
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

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedExchange = await m_dialogService.ShowUpdateExchangeAsync(SelectedItem);
        if (updatedExchange != null)
          m_itemsService.Update(updatedExchange);
        //TBD: Might have to update the item in the items list to make it reflect changes.
      }
    }

    public Exchange? GetItem(Guid id)
    {
      return Items.FirstOrDefault(x => x.Id == id);
    }
  }
}
