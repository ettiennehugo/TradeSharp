using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Interface to implement working with the set of available data providers.
  /// </summary>
  public interface IDataProviderRepository : IReadOnlyRepository<IPluginConfiguration, string> { }
}
