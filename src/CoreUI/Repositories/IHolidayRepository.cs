using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Concreate data store interface to query and edit country/exchange holiday data in an asychronous fashion so as to not tie up the UI thread.
  /// </summary>
  public interface IHolidayRepository : IReadOnlyRepository<Holiday, Guid>, IEditableRepository<Holiday, Guid>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid ParentId { get; set; }

    //methods

  }
}
