using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Collections.Generic;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Service used to host the plugins used to extend TradeSharp.
  /// </summary>
  public class PluginService : ServiceBase, IPluginService
  {
    //constants


    //enums


    //types


    //attributes
    protected IConfigurationService m_configurationService;
    protected ILogger<PluginService> m_logger;
    protected IHost m_host;

    //constructors
    public PluginService(IDialogService dialogService, ILogger<PluginService> logger, IConfigurationService configurationService): base(dialogService) 
    {
      m_configurationService = configurationService;
      m_logger = logger;
    }

    //finalizers


    //interface implementations


    //properties
    public IList<IPlugin> Plugins { get; internal set; }

    //methods
    //TBD: If this method of getting and instantiating plugins are too slow a lazy loading mechanism can be implemented that
    //     only lists the available plugins and instantiates them when they are needed.
    public void LoadPlugins(IHost host) 
    {
      m_host = host;
      Plugins = new List<IPlugin>();

      //InteractiveBrokers
      IPlugin plugin = (IPlugin)new TradeSharp.InteractiveBrokers.DataProviderPlugin();
      plugin.ServiceHost = host;
      plugin.Configuration = m_configurationService.DataProviders[plugin.Name];
      plugin.Create(m_logger);
      Plugins.Add(plugin);

      plugin = (IPlugin)new TradeSharp.InteractiveBrokers.BrokerPlugin();
      plugin.ServiceHost = host;
      plugin.Configuration = m_configurationService.DataProviders[plugin.Name];
      plugin.Create(m_logger);
      Plugins.Add(plugin);


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
  }
}
