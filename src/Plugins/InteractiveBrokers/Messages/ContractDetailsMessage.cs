using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
    public class ContractDetailsMessage
    {
        public ContractDetailsMessage(int requestId, IBApi.ContractDetails contractDetails)
        {
            RequestId = requestId;
            ContractDetails = contractDetails;
        }

        public IBApi.ContractDetails ContractDetails { get; set; }

        public int RequestId { get; set; }
    }
}
