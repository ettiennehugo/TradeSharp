using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class CompletedOrderMessage
    {
        public CompletedOrderMessage(Contract contract, IBApi.Order order, OrderState orderState)
        {
            Contract = contract;
            Order = order;
            OrderState = orderState;
        }

        public Contract Contract { get; set; }

        public IBApi.Order Order { get; set; }

        public OrderState OrderState { get; set; }
    }
}
