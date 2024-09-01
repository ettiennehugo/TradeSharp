using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis
{
    /// <summary>
    /// Interface for a the configuration object used to compose an analyss engine.
    /// </summary>
    public interface IEngineConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IList<IFilter> Filters { get; }

    //methods
    /// <summary>
    /// Add/remove operations.
    /// </summary>
    void Append(IFilter filter);
    bool InsertBefore(IFilter beforeFilter, IFilter filterToInsert);
    bool InsertAfter(IFilter afterFilter, IFilter filterToInsert);
    void Remove(IFilter filter);

    /// <summary>
    /// Sets up the engine composition before it is executed.
    /// </summary>
    void Compose(IEngine engine);
  }
}
