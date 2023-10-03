using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for objects that can update themselves from another instance (typically a clone during update operation).
  /// </summary>
  public interface IUpdateable<TItem>
    where TItem : class
  {
    /// <summary>
    /// Object should update itself from item except for it's key components.
    /// </summary>
    void Update(TItem item);
  }
}
