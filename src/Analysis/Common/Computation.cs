using TradeSharp.Data;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Base class for computations that can be perfomed by the analysis engine.
  /// </summary>
  public class Computation : IComputation
    {
        //constants


        //enums


        //types


        //attributes
        protected List<DataFeed> m_dataFeeds;

        //constructors
        public Computation()
        {
            m_dataFeeds = new List<DataFeed>();
        }

        //finalizers


        //interface implementations
        public void OnCalculate()
        {
            throw new NotImplementedException();
        }

        public void OnCreate()
        {
            throw new NotImplementedException();
        }

        public void OnDestroy()
        {
            throw new NotImplementedException();
        }

        public void OnStart()
        {
            throw new NotImplementedException();
        }

        public void OnStop()
        {
            throw new NotImplementedException();
        }

        //properties
        public IReadOnlyList<IDataFeed> DataFeeds => m_dataFeeds;

        //methods

    }
}
