using System.Text.Json.Serialization;

namespace TradeSharp.Common
{
	/// <summary>
	/// Individual tag entry to store in the tag information for a TradeSharp object.
	/// </summary>
  public class TagEntry
  {
		//constants


		//enums


		//types


		//attributes


		//properties
		public string Provider { get; set; } = string.Empty;
		public TagEntryVersion Version { get; set; } = new TagEntryVersion();

		[JsonConverter(typeof(RawJsonConverter))]			//value field should contain raw JSON data
		public string Value { get; set; } = string.Empty;

		//constructors


		//finalizers


		//interface implementations


		//methods



	}
}
