using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for instrument group service.
  /// </summary>
  public interface IInstrumentGroupService : ITreeItemsService<Guid, InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    /// <summary>
    /// Refresh the instrument tickers associated with the instrument group.
    /// </summary>
    void RefreshAssociatedTickers(InstrumentGroup instrumentGroup, bool force = false);
  }
}
