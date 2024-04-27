using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Views
{
  /// <summary>
  /// Dialog to display a list of corrective actions that can be taken to fix issues found.
  /// </summary>
  public interface ICorrectiveLoggerDialog : ICorrectiveLogger
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    void ShowAsync();
    void Close();
  }
}
