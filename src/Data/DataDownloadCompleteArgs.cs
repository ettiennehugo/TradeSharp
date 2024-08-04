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
		public DateTime? First { get; protected set; }
		public DateTime? Last { get; protected set; }

		//constructors
		public DataDownloadCompleteArgs(Instrument instrument, Resolution resolution, long count, DateTime? first, DateTime? last)
    {
      Instrument = instrument;
      Resolution = resolution;
      Count = count;
			First = first;
			Last = last;
    }

		//finalizers


		//interface implementations


		//methods


	}
}
