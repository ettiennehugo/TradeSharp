using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class SymbolSamplesMessage
    {
        public int ReqId { get; private set; }
        public ContractDescription[] ContractDescriptions { get; private set; }

        public SymbolSamplesMessage(int reqId, ContractDescription[] contractDescriptions)
        {
            ReqId = reqId;
            ContractDescriptions = contractDescriptions;
        }
    }
}
