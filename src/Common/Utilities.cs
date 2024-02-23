namespace TradeSharp.Common
{
  /// <summary>
  /// General utilities used throughout the application.
  /// </summary>
  public class Utilities
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties


    //methods
    public static string SafeFileName(string fileName)
    {
      return fileName.Replace(":", "_").Replace("/", "_").Replace("\\", "_").Replace(" ", "_");
    }

    /// <summary>
    /// Generate list of strings from the csv text.
    /// </summary>
    public static IList<string> FromCsv(string value)
    {
      if (string.IsNullOrEmpty(value)) return new List<string>();
      var csv = new List<string>();
      string sb = "";
      bool inQuote = false;

      for (int i = 0; i < value.Length; i++)
        if (value[i] == ',' && !inQuote)
        {
          csv.Add(sb);
          sb = "";
        }
        else if (value[i] == '"')
        {
          inQuote = !inQuote;
        }
        else
        {
          sb.Append(value[i]);
        }

      return csv;
    }

    /// <summary>
    /// Generate csv line from list of strings.
    /// </summary>
    public static string ToCsv(IList<string> values)
    {
      if (values.Count == 0) return "";
      string result = "";

      for (int i = 0; i < values.Count; i++)
      {
        result += values[i];
        if (i < values.Count - 1) result += ",";
      }

      return result;
    }
  }
}
