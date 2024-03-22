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
      IDataStoreConfiguration dataStoreConfiguration = (IDataStoreConfiguration)m_configurationService.General[IConfigurationService.GeneralConfiguration.Database];
      Assert.AreEqual(dataStoreConfiguration.Assembly, "TestDataStore,TestDataStore.dll", "Test data store assembly is incorrect");
      Assert.AreEqual(dataStoreConfiguration.ConnectionString, "TestDataStore.db", "Test data store connection string is incorrect");
      Assert.AreEqual((IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone], IConfigurationService.TimeZone.Local, "TimeZone value is not correct");
    }

    [TestMethod]
    public void DataProviders_CheckParsing_Success()
    {
      Assert.AreEqual(m_configurationService.DataProviders.Count, 2, "Number of data providers is incorrect");

      IPluginConfiguration dataProvider1 = m_configurationService.DataProviders.ElementAt(0).Value;
      Assert.AreEqual("TestDataProvider1", dataProvider1.Name, "Provider1 name is incorrect");
      Assert.AreEqual("TestDataProvider1Assembly", dataProvider1.Assembly, "Provider1 assembly is not correct");
      Assert.AreEqual("Provider1Value1", dataProvider1.Configuration["Provider1Key1"], "Provider1-Value1 is not correct");
      Assert.AreEqual("Provider1Value2", dataProvider1.Configuration["Provider1Key2"], "Provider1-Value2 is not correct");

      IPluginConfiguration dataProvider2 = m_configurationService.DataProviders.ElementAt(1).Value;
      Assert.AreEqual("TestDataProvider2", dataProvider2.Name, "Provider2 name is incorrect");
      Assert.AreEqual("TestDataProvider2Assembly", dataProvider2.Assembly, "Provider2 assembly is not correct");
      Assert.AreEqual("Provider2Value1", dataProvider2.Configuration["Provider2Key1"], "Provider2-Value1 is not correct");
      Assert.AreEqual("Provider2Value2", dataProvider2.Configuration["Provider2Key2"], "Provider2-Value2 is not correct");
    }

    [TestMethod]
    public void Brokerss_CheckParsing_Success()
    {
      Assert.AreEqual(m_configurationService.Brokers.Count, 1, "Number of brokers is incorrect");

      IPluginConfiguration broker1 = m_configurationService.Brokers.ElementAt(0).Value;
      Assert.AreEqual("TestBroker1", broker1.Name, "Broker1 name is incorrect");
      Assert.AreEqual("TestBroker1Assembly", broker1.Assembly, "Broker1 assembly is not correct");
      Assert.AreEqual("Broker1Value1", broker1.Configuration["Broker1Key1"], "Broker1-Value1 is not correct");
      Assert.AreEqual("Broker1Value2", broker1.Configuration["Broker1Key2"], "Broker1-Value2 is not correct");
    }
  }
}
