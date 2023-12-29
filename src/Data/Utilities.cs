using System.Collections.ObjectModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// General data utility functions. 
  /// </summary>
  public static class Utilities
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
    public static void Sort<T>(ObservableCollection<T> collection) where T : IComparable
    {
      List<T> sorted = collection.OrderBy(x => x).ToList();
      for (int i = 0; i < sorted.Count(); i++)
        collection.Move(collection.IndexOf(sorted[i]), i);
    }

    public static void SortedInsert<T>(T item, ObservableCollection<T> collection) where T : IComparable
    {
      for (int i = 0; i < collection.Count(); i++)
        if (item.CompareTo(collection[i]) <= 0)
        {
          collection.Insert(i, item);
          return;
        }
      collection.Add(item); //item larger than all others, add it to the end of collection
    }

    public static void UpdateItem<T>(T item, ObservableCollection<T> collection) where T : IEquatable<T>, IUpdateable<T>
    {
      for (int i = 0; i < collection.Count(); i++)
        if (item.Equals(collection[i]))
        {
          collection[i].Update(item);
          return;
        }
    }
  }
}
