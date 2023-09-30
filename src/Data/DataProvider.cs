using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base implementation for data provider.
  /// </summary>
  public abstract class DataProvider : IDataProvider
  {
    //constants


    //enums


    //types


    //attributes
    static protected Regex s_nameRegEx;
    IDataStoreService m_dataStore;

    //constructors
    static DataProvider()
    {
      //DataProvider name must be database safe since it's used in naming database tables.
      s_nameRegEx = new Regex(@"^[a-zA-Z][a-zA-Z0-9_\s,]*$");
    }

    public DataProvider(IDataStoreService dataStore, string name) : base()
    {
      if (!s_nameRegEx.IsMatch(name)) throw new ArgumentException(string.Format("DataProvider name \"{0}\" is invalid, must be only alphanumeric characters and start with alphabetical character.", name));
      Name = name;
      m_dataStore = dataStore;
    }

    //finalizers


    //interface implementations
    public abstract void Connect();
    public abstract void Create(string config);
    public abstract void Destroy();
    public abstract void Disconnect();
    public abstract void Dispose();
    public abstract void Request(string ticker, DateTime start, DateTime end);

    //properties
    public string Name { get; }
    public IList<string> Tickers => throw new NotImplementedException();    //this should refer directly into the data manager

    //methods


  }
}
