namespace TradeSharp.Common
{
	/// <summary>
	/// Event arguments for request errors.
	/// </summary>
  public class RequestErrorArgs
  {
		//constants


		//enums


		//types


		//attributes


		//properties
		public string Message { get; }
		public Exception? Exception { get; }

		//constructors
		public RequestErrorArgs(string message, Exception? exception = null)
    {
      Message = message;
      Exception = exception;
    }

		//finalizers


		//interface implementations


		//methods



	}
}
