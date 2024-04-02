using CommunityToolkit.Mvvm.Input;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
	/// <summary>
	/// Concrete interface for the plugins view model.
	/// </summary>
  public interface IPluginsViewModel : IListViewModel<IPlugin>
  {
		//constants


		//enums


		//types


		//attributes


		//properties
		public AsyncRelayCommand ConnectCommandAsync { get; set; }
		public AsyncRelayCommand DisconnectCommandAsync { get; set; }
		public AsyncRelayCommand SettingsCommandAsync { get; set; }

		//methods
		Task OnConnectAsync();
		Task OnDisconnectAsync();
		Task OnSettingsAsync();
	}
}
