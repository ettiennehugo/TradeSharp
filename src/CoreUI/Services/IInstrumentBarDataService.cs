using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for instrument bar data service.
  /// </summary>
  public interface IInstrumentBarDataService : IInstrumentDataService, IListItemsService<IBarData> { }
}
