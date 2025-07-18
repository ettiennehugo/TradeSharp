﻿namespace TradeSharp.InteractiveBrokers.Messages
{
  public class HistoricalTickMessage
    {
        public int ReqId { get; private set; }
        public long Time { get; private set; }
        public double Price { get; private set; }
        public decimal Size { get; private set; }

        public HistoricalTickMessage(int reqId, long time, double price, decimal size)
        {
            ReqId = reqId;
            Time = time;
            Price = price;
            Size = size;
        }
    }
}
