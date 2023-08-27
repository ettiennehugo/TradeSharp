using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// PrimaryExchange specific holiday, e.g. bank holidays where trading does not take place.
  /// </summary>
  public class ExchangeHoliday : Holiday, IExchangeHoliday
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExchangeHoliday(IDataStoreService dataStore, IDataManagerService dataManager, IExchange exchange, string name, Months month, int dayOfMonth, MoveWeekendHoliday moveWeekendHoliday) : base(dataStore, dataManager, exchange.Country, name, month, dayOfMonth, moveWeekendHoliday)
    {
      Exchange = exchange;
    }

    public ExchangeHoliday(IDataStoreService dataStore, IDataManagerService dataManager, IExchange exchange, string name, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday) : base(dataStore, dataManager, exchange.Country, name, month, dayOfWeek, weekOfMonth, moveWeekendHoliday)
    {
      Exchange = exchange;
    }

    public ExchangeHoliday(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Holiday holiday) : base(dataStore, dataManager, holiday) 
    {
      Exchange = ExchangeNone.Instance;
    }

    //finalizers


    //interface implementations


    //properties
    public IExchange Exchange { get; set; }

    //methods
    public override bool Equals(object? obj)
    {
      //holidays are name equivalent in an exchange
      return obj is ExchangeHoliday holiday &&
             EqualityComparer<IExchange>.Default.Equals(Exchange, holiday.Exchange) &&
             Name.Trim().ToUpper() == holiday.Name.ToUpper();
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Exchange, Name.ToUpper());
    }
  }
}
