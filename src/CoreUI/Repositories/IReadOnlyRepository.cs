namespace TradeSharp.Common
{
  /// <summary>
  /// Functional interface for classes that facilitate read-only access to the data store for specific object types using a specific key type. 
  /// </summary>
  public interface IReadOnlyRepository<T, in TKey>
    where T : class
  {

    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    T? GetItem(TKey id);
    IList<T> GetItems();
  }
}
