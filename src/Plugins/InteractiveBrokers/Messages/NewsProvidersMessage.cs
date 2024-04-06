using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class NewsProvidersMessage
    {
        public NewsProvider[] NewsProviders { get; private set; }

        public NewsProvidersMessage(NewsProvider[] newsProviders)
        {
            NewsProviders = newsProviders;
        }
    }
}
