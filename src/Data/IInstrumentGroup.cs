using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for groups of instruments, represents a tree of instrument groupings and their associated instruments that can be grouped together to allow analysis on hierarchical groups of instruments and individual instruments. Specifically targeted at supporting
  /// analysis of instruments relative to other groups of instruments.
  /// </summary>
  public interface IInstrumentGroup : IName, IDescription
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid Id { get; }
    IInstrumentGroup Parent { get; internal set; }
    IList<IInstrumentGroup> Children { get; }
    IList<IInstrument> Instruments { get; }

    //methods


  }
}
