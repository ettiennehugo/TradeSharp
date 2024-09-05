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


    //methods
    /// <summary>
    /// Composes the engine pipelines for data processing.
    /// </summary>
    void Compose(IEngine engine);
  }
}
