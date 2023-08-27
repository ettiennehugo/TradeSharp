using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace TradeSharp.Common
{
  /// <summary>
  /// Configuration service for the application
  /// </summary>

  public class ConfigurationService : IConfigurationService
  {
    //constants
    //configuration sections
    public const string c_tokenGeneral = "general";
    public const string c_tokenDataProviders = "dataproviders";
    public const string c_tokenDataStores = "datastores";
    public const string c_tokenBrokers = "brokers";
    public const string c_tokenExtensions = "extensions";

    //tokens for the data store type
    public const string c_tokenDataStoreTypename = "typename";
    public const string c_tokenDataStoreConnectionString = "connectionstring";

    //types


    //attributes
    protected IConfiguration m_configuration;

    //properties
    public CultureInfo CultureInfo { get; internal set; }
    public RegionInfo RegionInfo { get; internal set; }
    public IList<CultureInfo> CultureFallback { get; internal set; }
    public IDictionary<string, object> General { get; internal set; }
    public IDictionary<string, string> DataProviders { get; internal set; }
    public IDictionary<string, string> Brokers { get; internal set; }
    public IDictionary<string, string> Extensions { get; internal set; }

    //constructors
    public ConfigurationService(IConfiguration configuration)
    {
      m_configuration = configuration;
      CultureInfo = CultureInfo.CurrentCulture;
      RegionInfo = new RegionInfo(CultureInfo.LCID);
      General = new Dictionary<string, object>();
      CultureFallback = new List<CultureInfo>();
      DataProviders = new Dictionary<string, string>();
      Brokers = new Dictionary<string, string>();
      Extensions = new Dictionary<string, string>();
      setDefaults();
      loadConfiguration();
    }

    //finalizers


    //interface implementations


    //methods


    //attributes


    //methods
    /// <summary>
    /// Setup the default settings for the configuration.
    /// </summary>
    protected void setDefaults()
    {
      CultureFallback.Add(CultureInfo.CurrentCulture);
      CultureFallback.Add(new CultureInfo("en-US"));
      CultureFallback.Add(CultureInfo.InvariantCulture);
      General.Add(IConfigurationService.GeneralConfiguration.TimeZone, IConfigurationService.TimeZone.Local);
    }

    /// <summary>
    /// Load the configuration from the configuration object.
    /// </summary>


    //TODO: Allow use of environment variables in the configuration.
    // https://learn.microsoft.com/en-us/dotnet/api/system.environment.getenvironmentvariable?view=net-7.0


    protected void loadConfiguration()
    {
      var generalSection = m_configuration.GetSection(c_tokenGeneral);
      if (generalSection != null)
      {
        foreach (IConfigurationSection generalSetting in generalSection.GetChildren())
        {
          switch (generalSetting.Key)
          {
            case IConfigurationService.GeneralConfiguration.CultureFallback:
              CultureFallback.Clear();
              foreach (var culture in generalSetting.Value!.Split(','))
                CultureFallback.Add(new CultureInfo(culture));
              if (CultureFallback.Count == 0) throw new ArgumentException(string.Format(Resources.ParameterNotFound, IConfigurationService.GeneralConfiguration.CultureFallback));
              CultureInfo = CultureFallback[0];
              break;
            case IConfigurationService.GeneralConfiguration.TimeZone:
              General[IConfigurationService.GeneralConfiguration.TimeZone] = Enum.Parse(typeof(IConfigurationService.TimeZone), generalSetting.Value ?? "Local");
              break;
            case IConfigurationService.GeneralConfiguration.DataStore:
              var setting = new IConfigurationService.DataStoreConfiguration();

              foreach (IConfigurationSection subSetting in generalSetting.GetChildren())
                switch (subSetting.Key)
                {
                  case c_tokenDataStoreTypename:
                    setting.Typename = subSetting.Value!;
                    break;
                  case c_tokenDataStoreConnectionString:
                    setting.ConnectionString = subSetting.Value!;
                    break;
                }
              General[IConfigurationService.GeneralConfiguration.DataStore] = setting;
              break;
          }
        }
      }

      IConfigurationSection dataProvidersSection = m_configuration.GetRequiredSection(c_tokenDataProviders);
      if (dataProvidersSection != null)
        foreach (var dataProvider in dataProvidersSection.GetChildren())
          DataProviders[dataProvider.Key] = dataProvider.Value!;
      
      IConfigurationSection brokersSection = m_configuration.GetSection(c_tokenBrokers);
      if (brokersSection != null)
        foreach (var broker in brokersSection.GetChildren())
          Brokers[broker.Key] = broker.Value!;

      IConfigurationSection extensionsSection = m_configuration.GetSection(c_tokenExtensions);
      if (extensionsSection != null)
        foreach (var extension in extensionsSection.GetChildren())
          Extensions[extension.Key] = extension.Value!;
    }
  }
}
