using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class implementation for trading session on an exchange.
  /// </summary>
  public class Session : NameObject, ISession
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public Session(IDataStoreService dataStore, IDataManagerService dataManager, IExchange exchange, DayOfWeek day, string name, TimeOnly start, TimeOnly end) : base(dataStore, dataManager, name)
    {
      Exchange = exchange;
      Day = day;
      Start = start;
      End = end;      
      //NOTE: The session is inserted into the PrimaryExchange by the DataManager.
    }

    public Session(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Session session) : base(dataStore, dataManager, session.Name)
    {
      Id = session.Id;
      NameTextId = session.NameTextId;
      Name = DataStore.GetText(NameTextId);
      Exchange = ExchangeNone.Instance;
      Day = session.DayOfWeek;
      Start = session.Start;
      End = session.End;
      //NOTE: Data manager relinks the session to the PrimaryExchange.
    }

    //finalizers


    //interface implementations


    //properties
    public IExchange Exchange { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }

    //methods
    /// <summary>
    /// Sessions are name equivalent on the same exchange.
    /// </summary>
    public override bool Equals(object? obj)
    {
      return obj is Session session &&
             Exchange.Id == session.Exchange.Id &&
             Name.ToUpper() == session.Name.ToUpper();
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Name.ToUpper(), Exchange.Id);
    }
  }
}
