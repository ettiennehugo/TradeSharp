using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for instrument bar data service.
  /// </summary>
  public interface IInstrumentBarDataService : IInstrumentDataService, IListService<IBarData> { }
}
