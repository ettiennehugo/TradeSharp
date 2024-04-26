using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Dialog to show progress of a long running task, setting of properties must be thread safe.
  /// </summary>
  public interface IProgressDialog : ICorrectiveLogger
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    CancellationTokenSource CancellationTokenSource { get; }

    //NOTE: All these methods need to be thread safe and run the set methods on the UI thread since they would be called from the worker threads.
    double Minimum { get; set; }
    double Maximum { get; set; }
    double Progress { get; set; }
    bool Complete { get; set; }
    string StatusMessage { get; set; }

    //methods
    Task ShowAsync();
    void Close(bool cancelOperation);
  }
}
