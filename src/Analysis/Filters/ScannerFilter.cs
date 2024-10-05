using TradeSharp.Analysis.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation for the scanner filter to filter out instruments that do not meet specific criteria.
  /// </summary>
  public class ScannerFilter : Filter, IScannerFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public ScannerFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
