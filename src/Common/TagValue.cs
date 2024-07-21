using System.Text.Json;

namespace TradeSharp.Common
{
  /// <summary>
  /// Payload structure for the tag string used to record information from data providers.
  /// </summary>
  public class TagValue
  {
    //constants
    public const int IgnoreVersion = -1;
    public const string EmptyJson = "{\"Entries\":[]}";   //representation of empty JSON data

  //enums


  //types


  //attributes
  protected List<TagEntry> m_entries = new List<TagEntry>();

		//properties
		public IEnumerable<TagEntry> Entries { get => m_entries; }

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
      m_entries.Clear();
      foreach (var entry in value.Entries)
        m_entries.Add(entry);
    }

    //events
    public event EventHandler<TagEntry?>? EntriesChanged;

    //finalizers


    //interface implementations


    //methods
    public string ToJson()
    {
      return JsonSerializer.Serialize(this);
    }

    public bool HasEntry(string provider, int majorVersion, int minorVersion = IgnoreVersion, int patchVersion = IgnoreVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion && (minorVersion == IgnoreVersion || e.Version.Minor == minorVersion) && (e.Version.Patch == IgnoreVersion || e.Version.Patch == patchVersion)) != null;
    }

    public TagEntry? GetEntry(string provider, int majorVersion, int minorVersion = IgnoreVersion, int patchVersion = IgnoreVersion)
    {
      return Entries.FirstOrDefault((e) => e.Provider == provider && e.Version.Major == majorVersion && (minorVersion == IgnoreVersion || e.Version.Minor == minorVersion) && (e.Version.Patch == IgnoreVersion || e.Version.Patch == patchVersion));
    }

    public IList<TagEntry> GetEntries(string provider, int majorVersion, int minorVersion = IgnoreVersion, int patchVersion = IgnoreVersion)
    {
      return Entries.Where((e) => e.Provider == provider && e.Version.Major == majorVersion && (minorVersion == IgnoreVersion || e.Version.Minor == minorVersion) && (e.Version.Patch == IgnoreVersion || e.Version.Patch == patchVersion)).ToList();
    }

    /// <summary>
    /// Add/update the specified tag entry.
    /// </summary>
    public void Update(TagEntry entry)
    {
      m_entries.RemoveAll((e) => e.Provider == entry.Provider && e.Version.Major == entry.Version.Major && e.Version.Minor == entry.Version.Minor && e.Version.Patch == entry.Version.Patch);
      m_entries.Add(entry);
      EntriesChanged?.Invoke(this, entry);
    }

    public void Update(string provider, DateTime lastUpdated, int majorVersion, int minorVersion, int patchVersion, string value)
    {
      var tagEntry = new TagEntry();
      tagEntry.Provider = provider;
      tagEntry.LastUpdated = lastUpdated;
      tagEntry.Version.Major = majorVersion;
      tagEntry.Version.Minor = minorVersion;
      tagEntry.Version.Patch = patchVersion;
      tagEntry.Value = value;
      Update(tagEntry);
    }

    /// <summary>
    /// Remove all the entries matching the specified provider and version information.
    /// </summary>
    public void Remove(string provider, int majorVersion = IgnoreVersion, int minorVersion = IgnoreVersion, int patchVersion = IgnoreVersion)
    {
      int removeCount = m_entries.RemoveAll((e) => e.Provider == provider && (majorVersion == IgnoreVersion || e.Version.Major == majorVersion) && (minorVersion == IgnoreVersion || e.Version.Minor == minorVersion) && (e.Version.Patch == IgnoreVersion || e.Version.Patch == patchVersion));
      if (removeCount > 0)
        EntriesChanged?.Invoke(this, null);
    }

    /// <summary>
    /// Finds the best matching entry based on the provider and version information supplied.
    /// </summary>
    public TagEntry? BestMatch(string provider, int majorVersion, int minorVersion = -1, int patchVersion = -1)
    {
      TagEntry? result = null;
      var matchingEntries = Entries.Where((e) => e.Provider == provider && e.Version.Major <= majorVersion).ToList();

      foreach (var entry in matchingEntries)
      {
        if (entry.Version.Major == majorVersion)
        {
          if (entry.Version.Minor == minorVersion)
          {
            if (entry.Version.Patch == patchVersion)
            {
              result = entry;
              break;
            }
            else if (entry.Version.Patch < patchVersion)
            {
              result = entry;
            }
          }
          else if (entry.Version.Minor < minorVersion)
          {
            result = entry;
          }
        }
        else if (entry.Version.Major < majorVersion)
        {
          result = entry;
        }
      }

      return result;

    }
  }
}
