namespace TradeSharp.InteractiveBrokers.Messages
{
  public abstract class OrderMessage
    {
        protected int orderId;

        public int OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }
    }
}
