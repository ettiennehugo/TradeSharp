using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Concreate data store interface to query and edit instrument data in an asychronous fashion so as to not tie up the UI thread.
  /// </summary>
  public interface IInstrumentRepository: IReadOnlyRepository<Instrument, Guid>, IEditableRepository<Instrument, Guid> { }
}
