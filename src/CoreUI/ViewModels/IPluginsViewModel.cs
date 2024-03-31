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
		public RelayCommand ConnectCommand { get; set; }
		public RelayCommand DisconnectCommand { get; set; }
		public RelayCommand SettingsCommand { get; set; }

		//methods
		void OnConnect();
		void OnDisconnect();
		void OnSettings();
	}
}
