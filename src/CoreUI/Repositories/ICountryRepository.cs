﻿using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Concrete data store interface to query and edit country data in an asychronous fashion so as to not tie up the UI thread.
  /// </summary>
  public interface ICountryRepository : IReadOnlyRepository<Country, Guid>, IEditableRepository<Country, Guid> { }
}
