using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class FamilyCodesMessage
    {
        public FamilyCode[] FamilyCodes { get; private set; }

        public FamilyCodesMessage(FamilyCode[] familyCodes)
        {
            FamilyCodes = familyCodes;
        }
    }
}
