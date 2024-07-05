namespace TradeSharp.Data
{
	/// <summary>
	/// Arguments when data download is completed for an instrument.
	/// </summary>
  public class DataDownloadCompleteArgs
  {
		//constants


		//enums


		//types


		//attributes


		//properties
		public Instrument Instrument { get; protected set; }
		public Resolution Resolution { get; protected set; }
		public long Count { get; protected set; }

		//constructors
		public DataDownloadCompleteArgs(Instrument instrument, Resolution resolution, long count)
    {
      Instrument = instrument;
      Resolution = resolution;
      Count = count;
    }

		//finalizers


		//interface implementations


		//methods


	}
}
