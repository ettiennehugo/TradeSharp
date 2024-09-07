namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Run status used to facilitate the pipeline execution to either run perpetually till engine signals termination or
  /// the filters can signal that there will be no more data produced and thus engine termination is in order.
  /// </summary>
  public enum ExecutionStatus
  {
    Init,       //initialized and ready to run
    Running,    //processing messages
    Completed,  //completed processing - no more messages will be processed/produced by this filter/pipe/pipeline/engine
  }

  public class Types
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties


    //methods


  }
}
