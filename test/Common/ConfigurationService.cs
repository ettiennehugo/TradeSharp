using System.Globalization;

namespace Common
{
  [TestClass]
  public class ConfigurationService
  {
    //constants


    //enums


    //types


    //attributes
    private IConfigurationService m_configurationService;

    //constructors
    public ConfigurationService() 
    {
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");
      string jsonFile = string.Format("{0}\\{1}\\{2}", tradeSharpHome, Constants.ConfigurationDir, "tradesharp.test.json");
      m_configurationService = new TradeSharp.Common.ConfigurationService(jsonFile);
    }

    //finalizers


    //properties


    //methods
    [TestMethod]
    public void GeneralSettings_CheckParsing_Success() 
    {
      IDataStoreConfiguration dataStoreConfiguration = (IDataStoreConfiguration)m_configurationService.General[IConfigurationService.GeneralConfiguration.DataStore];
      Assert.AreEqual(dataStoreConfiguration.Assembly, "TestDataStore,TestDataStore.dll", "Test data store assembly is incorrect");
      Assert.AreEqual(dataStoreConfiguration.ConnectionString, "TestDataStore.db", "Test data store connection string is incorrect");
      Assert.AreEqual((IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone], IConfigurationService.TimeZone.Local, "TimeZone value is not correct");
    }

    [TestMethod]
    public void DataProviders_CheckParsing_Success()
    {
      Assert.AreEqual(m_configurationService.DataProviders.Count, 2, "Number of data providers is incorrect");

      IPluginConfiguration dataProvider1 = m_configurationService.DataProviders.ElementAt(0).Value;
      Assert.AreEqual(dataProvider1.Name, "TestDataProvider1", "Provider1 name is incorrect");
      Assert.AreEqual(dataProvider1.Assembly, "TestDataProvider1Assembly", "Provider1 assembly is not correct");
      Assert.AreEqual(dataProvider1.Profiles.Count, 2, "Provider1 profile count is incorrect");

      IPluginConfigurationProfile provider1Profile1 = dataProvider1.Profiles.ElementAt(0);
      Assert.AreEqual(provider1Profile1.Name, "Profile1", "Provider1 Profile1 name is incorrect");
      Assert.AreEqual(provider1Profile1.Description, "Profile1Description", "Provider1 Profile1 description is incorrect");
      Assert.AreEqual(provider1Profile1.Configuration.Count, 2, "Provider1 profile1 configuration parameter count is incorrect");
      KeyValuePair<string, object> configurationParameter = provider1Profile1.Configuration.ElementAt(0);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider1Profile1Key1", "Provider1 Profile1 Key1 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider1Profile1Value1", "Provider1 Profile1 Value1 is incorrect");
      configurationParameter = provider1Profile1.Configuration.ElementAt(1);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider1Profile1Key2", "Provider1 Profile1 Key2 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider1Profile1Value2", "Provider1 Profile1 Value2 is incorrect");

      IPluginConfigurationProfile provider1Profile2 = dataProvider1.Profiles.ElementAt(1);
      Assert.AreEqual(provider1Profile2.Name, "Profile2", "Provider1 Profile2 name is incorrect");
      Assert.AreEqual(provider1Profile2.Description, "Profile2Description", "Provider1 Profile2 description is incorrect");
      Assert.AreEqual(provider1Profile2.Configuration.Count, 3, "Provider1 Profile2 configuration parameter count is incorrect");
      configurationParameter = provider1Profile2.Configuration.ElementAt(0);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider1Profile2Key1", "Provider1 Profile2 Key1 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider1Profile2Value1", "Provider1 Profile2 Value1 is incorrect");
      configurationParameter = provider1Profile2.Configuration.ElementAt(1);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider1Profile2Key2", "Provider1 Profile2 Key2 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider1Profile2Value2", "Provider1 Profile2 Value2 is incorrect");
      configurationParameter = provider1Profile2.Configuration.ElementAt(2);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider1Profile2Key3", "Provider1 Profile2 Key3 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider1Profile2Value3", "Provider1 Profile2 Value3 is incorrect");

      IPluginConfiguration dataProvider2 = m_configurationService.DataProviders.ElementAt(1).Value;
      Assert.AreEqual(dataProvider2.Name, "TestDataProvider2", "Provider1 name is incorrect");
      Assert.AreEqual(dataProvider2.Assembly, "TestDataProvider2Assembly", "Provider1 assembly is not correct");
      Assert.AreEqual(dataProvider2.Profiles.Count, 2, "Provider2 profile count is incorrect");

      IPluginConfigurationProfile provider2Profile1 = dataProvider2.Profiles.ElementAt(0);
      Assert.AreEqual(provider2Profile1.Name, "Profile1", "Provider2 Profile1 name is incorrect");
      Assert.AreEqual(provider2Profile1.Description, "Profile1Description", "Provider2 Profile1 description is incorrect");
      Assert.AreEqual(provider2Profile1.Configuration.Count, 1, "Provider 1 profile 1 configuration parameter count is incorrect");
      configurationParameter = provider2Profile1.Configuration.ElementAt(0);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider2Profile1Key1", "Provider2 Profile1 Key1 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider2Profile1Value1", "Provider2 Profile1 Value1 is incorrect");

      IPluginConfigurationProfile provider2Profile2 = dataProvider2.Profiles.ElementAt(1);
      Assert.AreEqual(provider2Profile2.Name, "Profile2", "Provider2 Profile2 name is incorrect");
      Assert.AreEqual(provider2Profile2.Description, "Profile2Description", "Provider2 Profile2 description is incorrect");
      Assert.AreEqual(provider2Profile2.Configuration.Count, 4, "Provider2 Profile2 configuration parameter count is incorrect");
      configurationParameter = provider2Profile2.Configuration.ElementAt(0);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider2Profile2Key1", "Provider2 Profile2 Key1 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider2Profile2Value1", "Provider2 Profile2 Value1 is incorrect");
      configurationParameter = provider2Profile2.Configuration.ElementAt(1);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider2Profile2Key2", "Provider2 Profile2 Key2 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider2Profile2Value2", "Provider2 Profile2 Value2 is incorrect");
      configurationParameter = provider2Profile2.Configuration.ElementAt(2);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider2Profile2Key3", "Provider2 Profile2 Key3 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider2Profile2Value3", "Provider2 Profile2 Value3 is incorrect");
      configurationParameter = provider2Profile2.Configuration.ElementAt(3);
      Assert.AreEqual(configurationParameter.Key, "TestDataProvider2Profile2Key4", "Provider2 Profile2 Key4 is incorrect");
      Assert.AreEqual((string)configurationParameter.Value, "TestDataProvider2Profile2Value4", "Provider2 Profile2 Value4 is incorrect");
    }
  }
}
