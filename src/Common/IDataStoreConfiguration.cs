namespace TradeSharp.Common
{
  /// <summary>
  /// Interface for data store configurations.
  /// </summary>
  public interface IDataStoreConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Assembly and type used to access the data store implementation - implementation class must implement IDataStoreService.
    /// </summary>
    public string Assembly { get; set; }
    public string Type { get; set; }

    /// <summary>
    /// String used to configure the data store implementation - this is passed to the constructor of the IDataStoreService impleentation.
    /// </summary>
    public string ConnectionString { get; set; }

    //methods


  }
}
