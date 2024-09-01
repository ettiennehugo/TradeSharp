using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis
{
    /// <summary>
    /// Engine configuration that can be used to setup and compose an analysis engine in terms of it's
    /// IPipe and IFilter components.
    /// </summary>
    public class EngineConfiguration : IEngineConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public IList<IFilter> Filters { get; } = new List<IFilter>();

    //constructors


    //finalizers


    //interface implementations


    //methods
    public void Append(IFilter filter)
    {
      Filters.Add(filter);
    }

    public bool InsertBefore(IFilter beforeFilter, IFilter filterToInsert)
    {
      var index = Filters.IndexOf(beforeFilter);
      if (index < 0)
        return false;
      Filters.Insert(index, filterToInsert);
      return true;
    }

    public bool InsertAfter(IFilter afterFilter, IFilter filterToInsert)
    {
      var index = Filters.IndexOf(afterFilter);
      if (index < 0)
        return false;
      Filters.Insert(index + 1, filterToInsert);
      return true;
    }

    public void Remove(IFilter filter)
    {
      Filters.Remove(filter);
    }

    public void Compose(IEngine engine)
    {
      throw new NotImplementedException();
    }
  }
}
