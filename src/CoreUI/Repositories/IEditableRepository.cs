namespace TradeSharp.Common
{
  /// <summary>
  /// Functionl interface to allow editibility of the data store for a specific type using a specific key type. 
  /// </summary>
  public interface IEditableRepository<T, in TKey>
    where T : class
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    bool Add(T item);
    bool Update(T item);
    bool Delete(T item);
  }
}
