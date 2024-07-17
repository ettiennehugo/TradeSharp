using System.Text.Json;

namespace TradeSharp.Common
{
  /// <summary>
  /// Payload structure for the tag string used to record information from data providers.
  /// </summary>
  public class TagValue
  {
		//constants


		//enums


		//types


		//attributes


		//properties
		public List<TagEntry> Entries { get; } = new List<TagEntry>();

    //constructors
    public static TagValue? From(string json)
    {
      //special case when we're loading from the database
      if (string.IsNullOrEmpty(json))
        return new TagValue();

      //try to parse the value to a TagValue object
      try
      {
        return JsonSerializer.Deserialize<TagValue>(json);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public TagValue() { }   //default constructor

    public TagValue(TagValue value) //copy constructor
    {
      Entries.Clear();
      foreach (var entry in value.Entries)
        Entries.Add(entry);
    }

    //finalizers


    //interface implementations


    //methods
    public string ToJson()
    {
      return JsonSerializer.Serialize(this);
    }

    bool HasEntry(string provider)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider) != null;
    }

    bool HasEntry(string provider, int majorVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion) != null;
    }

    bool HasEntry(string provider, int majorVersion, int minorVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion && e.Version.Minor == minorVersion) != null;
    }

    bool HasEntry(string provider, int majorVersion, int minorVersion, int patchVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion && e.Version.Minor == minorVersion && e.Version.Patch == patchVersion) != null;
    }

    TagEntry? GetEntry(string provider)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider);
    }

    TagEntry? GetEntry(string provider, int majorVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion);
    }

    TagEntry? GetEntry(string provider, int majorVersion, int minorVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion && e.Version.Minor == minorVersion);
    }

    TagEntry? GetEntry(string provider, int majorVersion, int minorVersion, int patchVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion && e.Version.Minor == minorVersion && e.Version.Patch == patchVersion);
    }
  }
}
