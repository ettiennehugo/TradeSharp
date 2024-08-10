using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Commands
{
  /// <summary>
  /// State of a command.
  /// </summary>
  public enum CommandState
  {
    NotStarted,
    Running,
    Paused,
    Completed,
    Failed
  }

  /// <summary>
  /// Interface for command objects that encapsulate code performed by the application.
  /// </summary>
  public interface ICommand
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public CommandState State { get; }    

    //constructors


    //finalizers


    //methods
    Task StartAsync(IProgressDialog progressDialog, object? context);
  }
}
