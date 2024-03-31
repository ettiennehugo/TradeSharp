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
      ConnectCommand = new RelayCommand(OnConnect, () => SelectedItem != null && !SelectedItem.IsConnected);
      DisconnectCommand = new RelayCommand(OnConnect, () => SelectedItem != null && SelectedItem.IsConnected);
      SettingsCommand = new RelayCommand(OnConnect, () => SelectedItem != null);
    }

    public RelayCommand ConnectCommand { get; set; }
    public RelayCommand DisconnectCommand { get; set; }
    public RelayCommand SettingsCommand { get; set; }

    //finalizers


    //interface implementations
    public void OnConnect()
    {
      SelectedItem?.Connect();
    }

    public void OnDisconnect()
    {
      SelectedItem?.Disconnect();
    }

    public void OnSettings()
    {
      //TODO: Develop a generic way to update the settings of the Plugin???
      //  This might be just to update the IPluginConfiguration object.
      throw new NotImplementedException("TODO: Implement way to configure settings/IPluginConfiguration");
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
      ConnectCommand.NotifyCanExecuteChanged();
      DisconnectCommand.NotifyCanExecuteChanged();
      SettingsCommand.NotifyCanExecuteChanged();
    }

    //properties


    //methods


  }
}
