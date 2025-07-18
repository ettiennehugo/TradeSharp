﻿using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Concreate data store interface to query and edit exchange data in an asychronous fashion so as to not tie up the UI thread.
  /// </summary>
  public interface IExchangeRepository : IReadOnlyRepository<Exchange, Guid>, IEditableRepository<Exchange, Guid> { }
}
