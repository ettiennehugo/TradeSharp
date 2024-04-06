using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class TickByTickBidAskMessage
    {
        public int ReqId { get; private set; }
        public long Time { get; private set; }
        public double BidPrice { get; private set; }
        public double AskPrice { get; private set; }
        public int BidSize { get; private set; }
        public int AskSize { get; private set; }
        public TickAttribBidAsk TickAttribBidAsk { get; private set; }

        public TickByTickBidAskMessage(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            ReqId = reqId;
            Time = time;
            BidPrice = bidPrice;
            AskPrice = askPrice;
            BidSize = bidSize;
            AskSize = askSize;
            TickAttribBidAsk = tickAttribBidAsk;
        }
    }
}
