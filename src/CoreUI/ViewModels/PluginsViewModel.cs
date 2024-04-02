using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  //View model to work with the plugins configured for TradeSharp.  
  public class PluginsViewModel : ListViewModel<IPlugin>, IPluginsViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public PluginsViewModel(IPluginsService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) 
    {
      ConnectCommandAsync = new AsyncRelayCommand(OnConnectAsync, () => SelectedItem != null && !SelectedItem.IsConnected);
      DisconnectCommandAsync = new AsyncRelayCommand(OnDisconnectAsync, () => SelectedItem != null && SelectedItem.IsConnected);
      SettingsCommandAsync = new AsyncRelayCommand(OnSettingsAsync, () => SelectedItem != null && SelectedItem.HasSettings);
    }

    public AsyncRelayCommand ConnectCommandAsync { get; set; }
    public AsyncRelayCommand DisconnectCommandAsync { get; set; }
    public AsyncRelayCommand SettingsCommandAsync { get; set; }

    //finalizers


    //interface implementations
    public override Task OnRefreshAsync()
    {
      return Task.Run(() => m_itemsService.Refresh());
    }

    public Task OnConnectAsync()
    {
      return Task.Run(() =>
      {
        SelectedItem?.Connect();
        NotifyCanExecuteChanged();    //major commands require re-evaluation of states for other buttons
      });
    }

    public Task OnDisconnectAsync()
    {
      return Task.Run(() =>
      {
        SelectedItem?.Disconnect();
        NotifyCanExecuteChanged();    //major commands require re-evaluation of states for other buttons
      });
    }

    public Task OnSettingsAsync()
    {
    return Task.Run(() =>
         {
        SelectedItem?.ShowSettings();
        NotifyCanExecuteChanged();    //major commands require re-evaluation of states for other buttons
      });
    }

    public override void OnAdd()
    {
      throw new NotImplementedException("Add supported via the TradeSharp configuration.");
    }

    public override void OnUpdate()
    {
      throw new NotImplementedException("Update not supported.");
    }

    protected override void NotifyCanExecuteChanged()
    {
      base.NotifyCanExecuteChanged();
      ConnectCommandAsync.NotifyCanExecuteChanged();
      DisconnectCommandAsync.NotifyCanExecuteChanged();
      SettingsCommandAsync.NotifyCanExecuteChanged();

      //allow update of selected item's custom commands
      if (SelectedItem != null)
        foreach (var command in SelectedItem.CustomCommands)
          if (command.Command != null) command.Command.NotifyCanExecuteChanged();
    }

    //properties


    //methods


  }
}
