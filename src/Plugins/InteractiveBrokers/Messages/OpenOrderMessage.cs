using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class OpenOrderMessage : OrderMessage
    {
        public OpenOrderMessage(int orderId, Contract contract, IBApi.Order order, OrderState orderState)
        {
            OrderId = orderId;
            Contract = contract;
            Order = order;
            OrderState = orderState;
        }
        
        public Contract Contract { get; set; }

        public IBApi.Order Order { get; set; }

        public OrderState OrderState { get; set; }
    }
}
