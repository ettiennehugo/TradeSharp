using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for instrument group service.
  /// </summary>
  public interface IInstrumentGroupService : ITreeService<Guid, InstrumentGroup> { }
}
