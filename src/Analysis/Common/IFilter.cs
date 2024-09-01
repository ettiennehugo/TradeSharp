namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Interface for the filter implementation for the analysis engine.
  /// </summary>
  public interface IFilter : IPipeOrFilter
    {
        //constants


        //enums


        //types


        //attributes


        //properties
        /// <summary>
        /// Is this the first filter in the pipeline?
        /// </summary>
        bool IsStart { get; protected internal set; }

        //methods
        /// <summary>
        /// Evaluate the state of the filter to produce output. Returns whether the filter
        /// produced output to be passed to the next filter in the pipeline.
        /// </summary>
        bool Evaluate();
    }
}
