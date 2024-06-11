using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class UpdatePortfolioMessage
    {
        public UpdatePortfolioMessage(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string account)
        {
            Contract = contract;
            Position = position;
            MarketPrice = marketPrice;
            MarketValue = marketValue;
            AverageCost = averageCost;
            UnrealizedPNL = unrealizedPNL;
            RealizedPNL = realizedPNL;
            Account = account;
        }

        public Contract Contract { get; set; }

        public double Position { get; set; }

        public double MarketPrice { get; set; }

        public double MarketValue { get; set; }

        public double AverageCost { get; set; }

        public double UnrealizedPNL { get; set; }

        public double RealizedPNL { get; set; }

        public string Account { get; set; }
    }
}
