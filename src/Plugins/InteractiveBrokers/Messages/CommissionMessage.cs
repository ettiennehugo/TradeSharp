using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class CommissionMessage
    {
        public CommissionMessage(CommissionReport commissionReport)
        {
            CommissionReport = commissionReport;
        }

        public CommissionReport CommissionReport { get; set; }
    }
}
