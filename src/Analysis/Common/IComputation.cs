using TradeSharp.Data;

namespace TradeSharp.Analysis.Common
{
    public interface IComputation
    {
        IReadOnlyList<IDataFeed> DataFeeds { get; }

        void OnCalculate();
        void OnCreate();
        void OnDestroy();
        void OnStart();
        void OnStop();
    }
}