using System.Collections.ObjectModel;
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
    protected PluginsToDisplay m_pluginsToDisplay;
    protected ObservableCollection<IPlugin> m_items;

    //constructors
    public PluginsViewModel(IPluginsService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) 
    {
      m_pluginsToDisplay = PluginsToDisplay.All;
      m_items = new ObservableCollection<IPlugin>(itemsService.Items);
    }

    //finalizers


    //interface implementations
    public override Task OnRefreshAsync()
    {
      return Task.Run(() => { 
        m_itemsService.Refresh();
        foreach (var plugin in m_itemsService.Items)
          plugin.UpdateCommands += OnUpdateCommands;

        //filter the plugins to display
        refresh();
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

    public void OnUpdateCommands(object? sender, EventArgs e)
    {
      NotifyCanExecuteChanged();
    }

    protected override void NotifyCanExecuteChanged()
    {
      base.NotifyCanExecuteChanged();

      //allow update of selected item's custom commands
      if (SelectedItem != null)
        foreach (var command in SelectedItem.Commands)
          if (command.Command != null) command.Command.NotifyCanExecuteChanged();
    }

    //properties
    public virtual PluginsToDisplay PluginsToDisplay { get => m_pluginsToDisplay; set { m_pluginsToDisplay = value; refresh(); } }
    public override IList<IPlugin> Items { get => m_items; set => throw new NotImplementedException("Do not set the PluginViewModel items - use PluginsToDisplay to filter the list."); }

    //methods
    protected void refresh()
    {
      m_items.Clear();
      foreach (var item in m_itemsService.Items)
        if (m_pluginsToDisplay == PluginsToDisplay.All)
          m_items.Add(item);
        else if (m_pluginsToDisplay == PluginsToDisplay.Brokers && item is IBrokerPlugin)
          m_items.Add(item);
        else if (m_pluginsToDisplay == PluginsToDisplay.DataProviders && item is IDataProviderPlugin)
          m_items.Add(item);
        else if (m_pluginsToDisplay == PluginsToDisplay.Extensions && item is IExtensionPlugin)
          m_items.Add(item);
    }

  }
}
