using TradeSharp.Common;
using CommunityToolkit.Mvvm.DependencyInjection;


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
    public DataProviderRepository() 
    {
      m_configurationService = (IConfigurationService)Ioc.Default.GetRequiredService<IConfigurationService>();
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public Task<IPluginConfiguration?> GetItemAsync(string id)
    {
      IPluginConfiguration? dataProvider = null;
      m_configurationService.DataProviders.TryGetValue(id, out dataProvider);
      return Task.FromResult(dataProvider);
    }

    public Task<IEnumerable<IPluginConfiguration>> GetItemsAsync()
    {
      return Task.Run(() => { return m_configurationService.DataProviders.Values.AsEnumerable(); } );
    }
  }
}
