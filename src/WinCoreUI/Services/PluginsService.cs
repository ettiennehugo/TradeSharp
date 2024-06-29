using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Service used to host the plugins used to extend TradeSharp.
  /// </summary>
  public class PluginsService : ServiceBase, IPluginsService
  {
    //constants


    //enums


    //types


    //attributes
    protected IConfigurationService m_configurationService;
    protected ILogger<PluginsService> m_logger;
    private IPlugin? m_selectedItem;

    //constructors
    public PluginsService(IDialogService dialogService, ILogger<PluginsService> logger, IConfigurationService configurationService): base(dialogService) 
    {
      m_configurationService = configurationService;
      m_logger = logger;
      m_selectedItem = null;
      Items = new List<IPlugin>();
    }

    //finalizers


    //interface implementations
    public bool Add(IPlugin item)
    {
      throw new NotImplementedException("Add plugin via configuration");
    }

    public bool Copy(IPlugin item)
    {
      throw new NotImplementedException("Copy of plugin's not supported");
    }

    public bool Delete(IPlugin item)
    {
      throw new NotImplementedException("Delete plugin in configuration");
    }

    //properties
    public IHost Host { get; set; }
    public IList<IPlugin> Items { get; set; }
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } } //plugin's do not have a parent
    public IPlugin SelectedItem 
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public event EventHandler<IPlugin> SelectedItemChanged;

    //methods
    //TBD: If this method of getting and instantiating plugins are too slow a lazy loading mechanism can be implemented that
    //     only lists the available plugins and instantiates them when they are needed.
    public void Refresh()
    {
      //NOTE: Plugins should always just loaded only once-off, we do not run refresh again after the first refresh.
      if (Items.Count > 0)
      {
        m_logger.LogWarning($"Plugin service refresh called multiple times, only one refresh should be done.");
        return;
      }

      //load the sets of defined plugin's from the configuration
      loadPlugins(m_configurationService.Brokers);
      loadPlugins(m_configurationService.DataProviders);
      loadPlugins(m_configurationService.Extensions);
    }

    public bool Update(IPlugin item)
    {
      throw new NotImplementedException();
    }

    protected void loadPlugins(IDictionary<string,IPluginConfiguration> plugins)
    {
      foreach (var pluginConfig in plugins)
      {
        try
        {
          var assembly = Assembly.LoadFrom(pluginConfig.Value.Assembly);
          var type = assembly.GetType(pluginConfig.Value.Type);
          if (type == null)
          {
            m_logger.LogError($"Could not find plugin type {pluginConfig.Value.Type} in assembly {pluginConfig.Value.Assembly}.");
            continue;
          }
          // Create an instance of the type
          var instance = Activator.CreateInstance(type) as IPlugin;
          if (instance == null)
          {
            m_logger.LogError($"Could not create an instance of plugin type {pluginConfig.Value.Type}.");
            continue;
          }

          // Assuming the instance needs to be configured and added to Items
          instance.ServiceHost = Host;
          instance.Configuration = pluginConfig.Value;
          instance.Create(m_logger);
          Items.Add(instance);
        }
        catch (Exception ex)
        {
          m_logger.LogError(ex, $"Error loading plugin {pluginConfig.Key} - {ex.Message}.");
        }
      }
    }
  }
}
