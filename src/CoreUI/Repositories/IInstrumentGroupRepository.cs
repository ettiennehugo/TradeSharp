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
  /// Interface for the instrument group repository.
  /// </summary>
  public interface IInstrumentGroupRepository : IReadOnlyRepository<InstrumentGroup, Guid>, IEditableRepository<InstrumentGroup, Guid> { }

}
