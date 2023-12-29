using TradeSharp.Common;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Interface to implement working with the set of available data providers.
  /// </summary>
  public interface IDataProviderRepository : IReadOnlyRepository<IPluginConfiguration, string> { }
}
