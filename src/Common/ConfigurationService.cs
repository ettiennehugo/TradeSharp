using System.Globalization;
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
    public const string c_tokenDataStoreAssembly = "assembly";
    public const string c_tokenDataStoreConnectionString = "connectionstring";

    //tokens for the data provider plugins
    public const string c_tokenDataProviderName = "name";
    public const string c_tokenDataProviderAssembly = "assembly";
    public const string c_tokenDataProviderProfileName = "name";
    public const string c_tokenDataProviderProfileDescription = "description";

    //types


    //attributes
    protected IConfiguration m_configuration;

    //properties
    public CultureInfo CultureInfo { get; internal set; }
    public RegionInfo RegionInfo { get; internal set; }
    public IDictionary<string, object> General { get; internal set; }
    public IDictionary<string, IPluginConfiguration> DataProviders { get; internal set; }
    public IDictionary<string, string> Brokers { get; internal set; }
    public IDictionary<string, string> Extensions { get; internal set; }

    //constructors
    #nullable disable
    public ConfigurationService()
    {
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");
      string jsonFile = string.Format("{0}\\{1}\\{2}", tradeSharpHome, Constants.ConfigurationDir, "tradesharp.json");
      init(jsonFile);
    }

    #nullable disable
    public ConfigurationService(string jsonFile)
    {
      init(jsonFile);
    }

    private void init(string jsonFile)
    {
      IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile(jsonFile, false, true);
      m_configuration = builder.Build();
      CultureInfo = CultureInfo.CurrentCulture;
      RegionInfo = new RegionInfo(CultureInfo.LCID);
      General = new Dictionary<string, object>();
      DataProviders = new Dictionary<string, IPluginConfiguration>();
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
      General.Add(IConfigurationService.GeneralConfiguration.TimeZone, IConfigurationService.TimeZone.Local);
    }

    /// <summary>
    /// Load the configuration from the configuration object.
    /// </summary>
    protected void loadConfiguration()
    {
      loadGeneral();
      loadDataProviders();
      loadBrokers();
      loadExtensions();
    }

    protected void loadGeneral()
    {
      var generalSection = m_configuration.GetSection(c_tokenGeneral);
      if (generalSection != null)
      {
        foreach (IConfigurationSection generalSetting in generalSection.GetChildren())
        {
          switch (generalSetting.Key)
          {
            case IConfigurationService.GeneralConfiguration.TimeZone:
              General[IConfigurationService.GeneralConfiguration.TimeZone] = Enum.Parse(typeof(IConfigurationService.TimeZone), generalSetting.Value ?? "Local");
              break;
            case IConfigurationService.GeneralConfiguration.Database:
              var setting = new DataStoreConfiguration();

              foreach (IConfigurationSection subSetting in generalSetting.GetChildren())
                switch (subSetting.Key.ToLower())
                {
                  case c_tokenDataStoreAssembly:
                    setting.Assembly = subSetting.Value!;
                    break;
                  case c_tokenDataStoreConnectionString:
                    setting.ConnectionString = subSetting.Value!;
                    break;
                }
              General[IConfigurationService.GeneralConfiguration.Database] = setting;
              break;
          }
        }
      }
    }

    protected void loadDataProviders()
    {
      IConfigurationSection dataProvidersSection = m_configuration.GetRequiredSection(c_tokenDataProviders);
      if (dataProvidersSection != null)
        //parse data provider definitions
        foreach (var dataProvider in dataProvidersSection.GetChildren())
        {
          string assembly = "";
          List<IPluginConfigurationProfile> configurationProfiles = new List<IPluginConfigurationProfile>();

          //parse data provider profile definitions
          foreach (var dataProviderSetting in dataProvider.GetChildren())
          {
            switch (dataProviderSetting.Key.ToLower())
            {
              case c_tokenDataProviderAssembly:
                assembly = dataProviderSetting.Value!;
                break;
              default:
                string description = dataProviderSetting.Key; //default description to the name of the profile
                Dictionary<string, object> configuration = new Dictionary<string, object>();

                //parse data provider profile parameters
                foreach (var dataProviderProfile in dataProviderSetting.GetChildren())
                {
                  switch (dataProviderProfile.Key.ToLower())
                  {
                    case c_tokenDataProviderProfileDescription:
                      description = dataProviderProfile.Value!;
                      break;
                    default:
                      //ENHANCEMENT: We only store the string values for each key, the API does allow for the return of other key types such as
                      //             numbers, dates or even arrays.
                      configuration.Add(dataProviderProfile.Key, dataProviderProfile.Value);
                      break;
                  }
                }

                configurationProfiles.Add(new PluginConfigurationProfile(dataProvider.Key, dataProviderSetting.Key, description, configuration));
                break;
            }
          }

          DataProviders[dataProvider.Key] = new PluginConfiguration(dataProvider.Key, assembly, configurationProfiles);
      }
    }

    private void loadExtensions()
    {
      IConfigurationSection brokersSection = m_configuration.GetSection(c_tokenBrokers);
      if (brokersSection != null)
        foreach (var broker in brokersSection.GetChildren())
          Brokers[broker.Key] = broker.Value!;
    }

    private void loadBrokers()
    {
      IConfigurationSection extensionsSection = m_configuration.GetSection(c_tokenExtensions);
      if (extensionsSection != null)
        foreach (var extension in extensionsSection.GetChildren())
          Extensions[extension.Key] = extension.Value!;
    }
  }
}
