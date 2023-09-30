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
    /// <summary>
    /// Show a message to the user.
    /// </summary>
    Task ShowMessageAsync(string message);

    /// <summary>
    /// Get a country iso-code from the user and return it.
    /// </summary>
    Task<CountryInfo?> ShowSelectCountryAsync();

    /// <summary>
    /// Creates a new holiday object for the given parent and returns it.
    /// </summary>
    Task<Holiday> ShowCreateHolidayAsync(Guid parentId);    

    /// <summary>
    /// Update existing holiday object and returns the updated object.
    /// </summary>
    Task<Holiday> ShowUpdateHolidayAsync(Holiday holiday);    
  }
}
