using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Common
{
  /// <summary>
  /// Implementation class to store the data store configuration.
  /// </summary>
  public class DataStoreConfiguration : IDataStoreConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public DataStoreConfiguration()
    {
      Assembly = string.Empty;
      ConnectionString = string.Empty;
    }

    public DataStoreConfiguration(string typename, string connectionString)
    {
      Assembly = typename;
      ConnectionString = connectionString;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public string Assembly { get; set; }
    public string ConnectionString { get; set; }
  }
}
