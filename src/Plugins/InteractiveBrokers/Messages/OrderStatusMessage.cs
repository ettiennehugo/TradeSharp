﻿namespace TradeSharp.InteractiveBrokers.Messages
{
  public class OrderStatusMessage : OrderMessage
    {
        public string Status { get; private set; }
        public double Filled { get; private set; }
        public double Remaining { get; private set; }
        public double AvgFillPrice { get; private set; }
        public int PermId { get; private set; }
        public int ParentId { get; private set; }
        public double LastFillPrice { get; private set; }
        public int ClientId { get; private set; }
        public string WhyHeld { get; private set; }
        public double MktCapPrice { get; private set; }

        public OrderStatusMessage(int orderId, string status, double filled, double remaining, double avgFillPrice,
           int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            OrderId = orderId;
            Status = status;
            Filled = filled;
            Remaining = remaining;
            AvgFillPrice = avgFillPrice;
            PermId = permId;
            ParentId = parentId;
            LastFillPrice = lastFillPrice;
            ClientId = clientId;
            WhyHeld = whyHeld;
        }       
    }
}
