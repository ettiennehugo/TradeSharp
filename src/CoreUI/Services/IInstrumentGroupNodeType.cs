using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for the instrument group node type.
  /// </summary>
  public interface IInstrumentGroupNodeType: ITreeNodeType<Guid, InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    bool InstrumentsVisible { get; set; }

    //methods


  }
}
