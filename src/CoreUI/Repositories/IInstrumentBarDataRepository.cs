using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Concrete data store interface to query and edit instrument data in an asychronous fashion so as to not tie up the UI thread.
  /// </summary>
  public interface IInstrumentBarDataRepository : IInstrumentDataRepository, IReadOnlyRepository<IBarData, DateTime>, IEditableRepository<IBarData, DateTime> { }
}
