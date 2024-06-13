
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  public class BrokerAccountsViewModel : TreeViewModel<string, object>, IBrokerAccountsViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public BrokerAccountsViewModel(IBrokerAccountsService itemService, INavigationService navigationService, IDialogService dialogService) : base(itemService, navigationService, dialogService) { }

    //finalizers


    //interface implementations


    //properties


    //methods
    public override void OnAdd()
    {
      throw new NotImplementedException();
    }

    public override void OnClearFilter()
    {
      throw new NotImplementedException();
    }

    public override Task OnCopyAsync(object? target)
    {
      throw new NotImplementedException();
    }

    public override void OnFindFirst()
    {
      throw new NotImplementedException();
    }

    public override void OnFindNext()
    {
      throw new NotImplementedException();
    }

    public override void OnFindPrevious()
    {
      throw new NotImplementedException();
    }

    public override void OnUpdate()
    {
      throw new NotImplementedException();
    }
  }
}
