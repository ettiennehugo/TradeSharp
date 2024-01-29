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
    /// Copy data to the subsequent higher resolution, Resolution.Month will do nothing.
    /// NOTE: This method operates on the data available in the repository, so, if the repository
    ///       is not populated with data, this method will produce incorrect results.
    /// </summary>
    void Copy(Resolution from);

  }
}
