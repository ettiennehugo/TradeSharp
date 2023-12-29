using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Interface for the instrument group repository.
  /// </summary>
  public interface IInstrumentGroupRepository : IReadOnlyRepository<InstrumentGroup, Guid>, IEditableRepository<InstrumentGroup, Guid> { }
}
