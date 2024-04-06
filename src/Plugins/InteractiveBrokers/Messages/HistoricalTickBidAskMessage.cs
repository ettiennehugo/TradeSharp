using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class HistoricalTickBidAskMessage
    {
        public int ReqId { get; set; }
        public long Time { get; set; }
        public TickAttribBidAsk TickAttribBidAsk { get; set; }
        public double PriceBid { get; set; }
        public double PriceAsk { get; set; }
        public decimal SizeBid { get; set; }
        public decimal SizeAsk { get; set; }

        public HistoricalTickBidAskMessage(int reqId, long time, TickAttribBidAsk tickAttribBidAsk, double priceBid, double priceAsk, decimal sizeBid, decimal sizeAsk)
        {
            ReqId = reqId;
            Time = time;
            TickAttribBidAsk = tickAttribBidAsk;
            PriceBid = priceBid;
            PriceAsk = priceAsk;
            SizeBid = sizeBid;
            SizeAsk = sizeAsk;
        }
    }
}
