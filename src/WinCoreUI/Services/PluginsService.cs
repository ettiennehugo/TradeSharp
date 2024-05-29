using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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

      //InteractiveBrokers
      IPlugin plugin = (IPlugin)new TradeSharp.InteractiveBrokers.DataProviderPlugin();
      plugin.ServiceHost = Host;
      plugin.Configuration = m_configurationService.DataProviders[plugin.Name];
      plugin.Create(m_logger);
      Items.Add(plugin);

      plugin = (IPlugin)new TradeSharp.InteractiveBrokers.BrokerPlugin();
      plugin.ServiceHost = Host;
      plugin.Configuration = m_configurationService.Brokers[plugin.Name];
      plugin.Create(m_logger);
      Items.Add(plugin);

      //TODO: Currently no extension plugins are implemented, but the following code should be used to load them.

      //TODO: Currently the plugins are loaded but you need to setup a static reference to the project that defines the plugin. This is not correct,
      //      and the plugins should be loaded dynamically without a static reference to the project. The following code is somewhat of what it should
      //      look like:
      //ObjectHandle? dataProviderHandle = Activator.CreateInstanceFrom(dataProviderPluginConfig.Value.Assembly, dataProviderPluginConfig.Value.Type);
      //if (dataProviderHandle == null)
      //{
      //  m_logger.LogError($"Error creating instance of data provider plugin {dataProviderPluginConfig.Key}");
      //  continue;
      //}

      //foreach (var dataProviderPluginConfig in m_configurationService.DataProviders)
      //{
      //  try
      //  {
      //    Type t = typeof(TradeSharp.InteractiveBrokers.DataProviderPlugin);
      //    string n = t.ToString();

      //    Type? dataProviderPluginType = Type.GetType(dataProviderPluginConfig.Value.Type);
      //    Plugin? dataProviderPlugin = (Plugin?)Activator.CreateInstance(t);
      //    if (dataProviderPlugin == null)
      //    {
      //      m_logger.LogError($"Error unwrapping instance of data provider plugin {dataProviderPluginConfig.Key}");
      //      continue;
      //    }

      //    dataProviderPlugin!.ServiceHost = host;
      //    dataProviderPlugin.Configuration = dataProviderPluginConfig.Value;
      //    dataProviderPlugin.Create(m_logger);
      //    Plugins.Add((IPlugin)dataProviderPlugin);
      //  }
      //  catch (Exception ex)
      //  {
      //    m_logger.LogError(ex, $"Error loading data provider plugin {dataProviderPluginConfig.Key}");
      //  }
      //}

      //foreach (var brokerPluginConfig in m_configurationService.Brokers) 
      //{
      //  try
      //  {
      //    ObjectHandle? brokerPluginHandle = Activator.CreateInstanceFrom(brokerPluginConfig.Value.Assembly, brokerPluginConfig.Value.Type);
      //    if (brokerPluginHandle == null)
      //    {
      //      m_logger.LogError($"Error creating instance of broker plugin {brokerPluginConfig.Key}");
      //      continue;
      //    }

      //    Plugin? brokerPlugin = (Plugin?)brokerPluginHandle.Unwrap();
      //    if (brokerPlugin == null)
      //    {
      //      m_logger.LogError($"Error unwrapping instance of broker plugin {brokerPluginConfig.Key}");
      //      continue;
      //    }

      //    brokerPlugin!.ServiceHost = host;
      //    brokerPlugin!.Configuration = brokerPluginConfig.Value;
      //    brokerPlugin.Create(m_logger);
      //    Plugins.Add((IPlugin)brokerPlugin);
      //  }
      //  catch (Exception ex)
      //  {
      //    m_logger.LogError(ex, $"Error loading broker plugin {brokerPluginConfig.Key}");
      //  }
      //}

      //foreach (var extensionPluginConfig in m_configurationService.Extensions) 
      //{
      //  try
      //  {
      //    ObjectHandle? extensionPluginHandle = Activator.CreateInstanceFrom(extensionPluginConfig.Value.Assembly, extensionPluginConfig.Value.Type);
      //    if (extensionPluginHandle == null)
      //    {
      //      m_logger.LogError($"Error creating instance of extension plugin {extensionPluginConfig.Key}");
      //      continue;
      //    }

      //    Plugin? extensionPlugin = (Plugin?)extensionPluginHandle.Unwrap();
      //    if (extensionPlugin == null)
      //    {
      //      m_logger.LogError($"Error unwrapping instance of extension plugin {extensionPluginConfig.Key}");
      //      continue;
      //    }

      //    extensionPlugin!.ServiceHost = host;
      //    extensionPlugin!.Configuration = extensionPluginConfig.Value;
      //    extensionPlugin.Create(m_logger);
      //    Plugins.Add((IPlugin)extensionPlugin);
      //  }
      //  catch (Exception ex)
      //  {
      //    m_logger.LogError(ex, $"Error loading extension plugin {extensionPluginConfig.Key}");
      //  }
      //}
    }

    public bool Update(IPlugin item)
    {
      throw new NotImplementedException();
    }
  }
}
