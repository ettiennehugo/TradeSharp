using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Object managed by a data manager.
  /// </summary>
  public class DataManagerObject : ObjectWithId
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public DataManagerObject(IDataStoreService dataStore, IDataManagerService dataManager)
    {
      DataStore = dataStore;
      DataManager = dataManager;
    }

    //finalizers


    //interface implementations


    //properties
    public IDataStoreService DataStore { get; }
    public IDataManagerService DataManager { get; }

    //methods



  }
}
