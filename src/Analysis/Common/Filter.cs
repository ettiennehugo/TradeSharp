namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Base class for filters in the analysis engine.
  /// </summary>
  public class Filter : PipeOrFilter, IFilter
    {
        //constants


        //enums


        //types


        //attributes


        //properties
        public bool IsStart { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //constructors


        //finalizers


        //interface implementations


        //methods
        public bool Evaluate()
        {
            throw new NotImplementedException();
        }
    }
}
