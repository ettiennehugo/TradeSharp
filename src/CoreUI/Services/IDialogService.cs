using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
    /// <summary>
    /// Interface for the platform independent dialog service.
    /// </summary>
    public interface IDialogService
  {
    //constants


    //enums


    //types
    

    //attributes


    //properties


    //methods
    Task ShowMessageAsync(string message);

    Task<CountryInfo?> ShowSelectCountryAsync();

    Task<Holiday?> ShowCreateHolidayAsync(Guid parentId);
    Task<Holiday?> ShowUpdateHolidayAsync(Holiday holiday);

    Task<Exchange?> ShowCreateExchangeAsync();
    Task<Exchange?> ShowUpdateExchangeAsync(Exchange exchange);

    Task<Session?> ShowCreateSessionAsync(Guid parentId);
    Task<Session?> ShowUpdateSessionAsync(Session session);
  }
}
