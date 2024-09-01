namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Base class of the pipe in the analysis engine.
  /// </summary>
  public class Pipe : PipeOrFilter, IPipe
    {
        //constants


        //enums


        //types


        //attributes


        //properties
        public IFilter Source => throw new NotImplementedException();
        public IFilter End => throw new NotImplementedException();

        //constructors


        //finalizers


        //interface implementations


        //methods


    }
}
