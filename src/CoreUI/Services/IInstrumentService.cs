using CommunityToolkit.Common.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for the instrument service.
  /// </summary>
  public interface IInstrumentService : IListItemsService<Instrument> { }
}
