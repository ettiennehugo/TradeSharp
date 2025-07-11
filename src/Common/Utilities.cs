﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

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
          inQuote = !inQuote;
        else
          sb += value[i];

      if (sb.Length > 0) csv.Add(sb); //need to catch the last element we were parsing

      return csv;
    }

    /// <summary>
    /// Make string value safe to use as a CSV value.
    /// </summary>
    public static string MakeCsvSafe(string value)
    {
      if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        return "\"" + value.Replace("\"", "\"\"") + "\"";
      else
        return value;
    }

    /// <summary>
    /// Generate csv line from list of strings.
    /// </summary>
    public static string ToCsv<T>(IList<T> values)
    {
      if (values.Count == 0) return "";
      string result = "";

      for (int i = 0; i < values.Count; i++)
      {
        result += values[i]!.ToString();
        if (i < values.Count - 1) result += ",";
      }

      return MakeCsvSafe(result);
    }

    public static void Sort<TSource>(IList<TSource> collection) where TSource : IComparable
    {
      List<TSource> sorted = collection.OrderBy(x => x).ToList();
      for (int i = 0; i < sorted.Count(); i++)
      {
        int oldIndex = collection.IndexOf(sorted[i]);
        TSource item = collection[oldIndex];
        collection.RemoveAt(oldIndex);
        collection.Insert(i, item);
      }
    }

    public static void Sort<TSource, TKey>(IList<TSource> collection, Func<TSource, TKey> keySelector) where TSource : IComparable
    {
      List<TSource> sorted = collection.OrderBy(keySelector).ToList();
      for (int i = 0; i < sorted.Count(); i++)
      {
        int oldIndex = collection.IndexOf(sorted[i]);
        TSource item = collection[oldIndex];
        collection.RemoveAt(oldIndex);
        collection.Insert(i, item);
      }
    }

    public static void SortedInsert<T>(T item, IList<T> collection) where T : IComparable
    {
      for (int i = 0; i < collection.Count(); i++)
        if (item.CompareTo(collection[i]) <= 0)
        {
          collection.Insert(i, item);
          return;
        }
      collection.Add(item); //item larger than all others, add it to the end of collection
    }
  }

  /// <summary>
  /// Used for serialization/deserialization of string fields that contain raw JSON.
  /// </summary>
  public class RawJsonConverter : JsonConverter<string>
  {
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using (var jsonDoc = JsonDocument.ParseValue(ref reader))
      {
        return jsonDoc.RootElement.GetRawText();
      }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
      using (var jsonDoc = JsonDocument.Parse(value))
      {
        jsonDoc.RootElement.WriteTo(writer);
      }
    }
  }
}
