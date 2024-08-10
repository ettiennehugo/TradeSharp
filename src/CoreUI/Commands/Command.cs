using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Commands
{
	/// <summary>
	/// Base class for command objects that encapsulate code performed by the application.
	/// </summary>
  public abstract partial class Command: ObservableObject, ICommand
  {
		//constants


		//enums


		//types


		//attributes
		protected IServiceProvider m_serviceHost;

    //properties
    [ObservableProperty] CommandState m_state;

    //constructors
    public Command() {
			State = CommandState.NotStarted;
			m_serviceHost = IApplication.Current.Services;
		}

		//finalizers


		//interface implementations
		public abstract Task StartAsync(IProgressDialog progressDialog, object? context);

    //methods


  }
}
