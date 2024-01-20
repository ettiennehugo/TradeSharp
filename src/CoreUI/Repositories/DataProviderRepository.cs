using TradeSharp.Common;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Implementation of the data provider repository.
  /// </summary>
  public class DataProviderRepository : IDataProviderRepository
  {
    //constants


    //enums


    //types


    //attributes
    private IConfigurationService m_configurationService;

    //constructors
    public DataProviderRepository(IConfigurationService configurationService) 
    {
      m_configurationService = configurationService;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public IPluginConfiguration? GetItem(string id)
    {
      IPluginConfiguration? dataProvider = null;
      m_configurationService.DataProviders.TryGetValue(id, out dataProvider);
      return dataProvider;
    }

    public IList<IPluginConfiguration> GetItems()
    {
      return m_configurationService.DataProviders.Values.ToList();
    }
  }
}
