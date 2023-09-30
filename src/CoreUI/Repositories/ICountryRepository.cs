using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common; 
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
    /// <summary>
    /// Concrete data store interface to query and edit country data in an asychronous fashion so as to not tie up the UI thread.
    /// The implementation can use the IDataStoreService or IDataManagerService to facilitate the actual underlying operations.
    /// </summary>
    public interface ICountryRepository : IReadOnlyRepository<Country, Guid>, IEditableRepository<Country, Guid> 
  {

    //constants


    //enums


    //types


    //attributes


    //properties


    //methods


  }
}
