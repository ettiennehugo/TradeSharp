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
		private Instrument m_instrument;
		private Resolution m_resolution;
		private long m_count;

		//properties
		public Instrument Instrument { get { return m_instrument; } }
		public Resolution Resolution { get { return m_resolution; } }
		public long Count { get { return m_count; } }

		//constructors
		public DataDownloadCompleteArgs(Instrument instrument, Resolution resolution, long count)
    {
      m_instrument = instrument;
      m_resolution = resolution;
      m_count = count;
    }

		//finalizers


		//interface implementations


		//methods


	}
}
