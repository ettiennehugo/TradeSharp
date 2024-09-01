namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Interface for the pipe implementation for the analysis engine.
  /// </summary>
  public interface IPipe : IPipeOrFilter
    {
        //constants


        //enums


        //types


        //attributes


        //properties
        /// <summary>
        /// Source and end filters of the pipe.
        /// </summary>
        IFilter Source { get; }
        IFilter End { get; }

        //methods


    }
}
