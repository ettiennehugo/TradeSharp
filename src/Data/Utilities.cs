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
    public static void UpdateItem<T>(T item, IList<T> collection) where T : IEquatable<T>, IUpdateable<T>
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
