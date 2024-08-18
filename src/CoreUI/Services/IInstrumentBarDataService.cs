using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for instrument bar data service.
  /// </summary>
  public interface IInstrumentBarDataService : IInstrumentDataService<IBarData>, IListService<IBarData> 
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    /// <summary>
    /// Refresh the service to the given date/time range.
    /// </summary>
    void Refresh(DateTime from, DateTime to);
    
    /// <summary>
    /// Copy data from a lower resolution to a higher resolution in the given date/time range.
    /// NOTE: This method operates on the data available in the repository, so, if the repository
    ///       is not populated with data, this method will produce incorrect results.
    /// </summary>
    void Copy(Resolution from, Resolution to, DateTime? fromDateTime = null, DateTime? toDateTime = null);
  }
}
