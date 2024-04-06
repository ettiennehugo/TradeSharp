using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class BondContractDetailsMessage
    {
        public BondContractDetailsMessage(int requestId, ContractDetails contractDetails)
        {
            RequestId = requestId;
            ContractDetails = contractDetails;
        }

        public ContractDetails ContractDetails { get; set; }

        public int RequestId { get; set; }
    }
}
