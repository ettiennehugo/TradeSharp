using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class TickByTickAllLastMessage
    {
        public int ReqId { get; private set; }
        public int TickType { get; private set; }
        public long Time { get; private set; }
        public double Price { get; private set; }
        public int Size { get; private set; }
        public TickAttribLast TickAttribLast { get; private set; }
        public string Exchange { get; private set; }
        public string SpecialConditions { get; private set; }

        public TickByTickAllLastMessage(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttribLast, string exchange, string specialConditions)
        {
            ReqId = reqId;
            TickType = tickType;
            Time = time;
            Price = price;
            Size = size;
            TickAttribLast = tickAttribLast;
            Exchange = exchange;
            SpecialConditions = specialConditions;
        }
    }
}
