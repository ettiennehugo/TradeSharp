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
  
    //tokens for the plugins
    public const string c_tokenAssembly = "assembly";
    public const string c_tokenType = "type";

    //tokens for the data store type
    public const string c_tokenDataStoreConnectionString = "connectionstring";

    //types


    //attributes
    protected IConfiguration m_configuration;

    //properties
    public CultureInfo CultureInfo { get; internal set; }
    public RegionInfo RegionInfo { get; internal set; }
    public IDictionary<string, object> General { get; internal set; }
    public IDictionary<string, IPluginConfiguration> DataProviders { get; internal set; }
    public IDictionary<string, IPluginConfiguration> Brokers { get; internal set; }
    public IDictionary<string, IPluginConfiguration> Extensions { get; internal set; }

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
      Brokers = new Dictionary<string, IPluginConfiguration>();
      Extensions = new Dictionary<string, IPluginConfiguration>();
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
      loadSection(c_tokenDataProviders, DataProviders);
      loadSection(c_tokenBrokers, Brokers);

      //load optional sections
      try { loadSection(c_tokenExtensions, Extensions); } catch { }

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
                  case c_tokenAssembly:
                    setting.Assembly = subSetting.Value!;
                    break;
                  case c_tokenType:
                    setting.Type = subSetting.Value!;
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

    protected void loadSection(string sectionName, IDictionary<string, IPluginConfiguration> configuration)
    {
      IConfigurationSection section = m_configuration.GetSection(sectionName);
      if (section != null)
        foreach (var subSection in section.GetChildren())
        {
          string assembly = "";
          string type = "";
          Dictionary<string, object> settings = new Dictionary<string, object>();

          //parse data provider profile definitions
          foreach (var subSectionSetting in subSection.GetChildren())
          {
            switch (subSectionSetting.Key.ToLower())
            {
              case c_tokenAssembly:
                assembly = subSectionSetting.Value!;
                break;
              case c_tokenType:
                type = subSectionSetting.Value!;
                break;
              default:
                //ENHANCEMENT: We only store the string values for each key, the API does allow for the return of other key types such as
                //             numbers, dates or even arrays.
                settings.Add(subSectionSetting.Key, subSectionSetting.Value);
                break;
            }
          }

          configuration[subSection.Key] = new PluginConfiguration(subSection.Key, assembly, type, settings);
        }
    }
  }
}
