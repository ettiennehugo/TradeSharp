using CommunityToolkit.Mvvm.Input;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Types of plugins to display.
  /// </summary>
  public enum PluginsToDisplay
  {
    All,
    Brokers,
    DataProviders,
    Extensions
  }

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
    public PluginsToDisplay PluginsToDisplay { get; set; }

		//methods

	}
}
