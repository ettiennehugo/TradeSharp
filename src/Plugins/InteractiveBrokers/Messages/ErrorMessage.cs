using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class ErrorMessage 
    {
        public ErrorMessage(int requestId, int errorCode, string message, string advancedOrderRejectJson)
        {
            AdvancedOrderRejectJson = advancedOrderRejectJson;
            Message = message;
            RequestId = requestId;
            ErrorCode = errorCode;
        }

        public string AdvancedOrderRejectJson { get; set; }

        public string Message { get; set; }

        public int ErrorCode { get; set; }


        public int RequestId { get; set; }

        public override string ToString()
        {
            string ret = "Error. Request: " + RequestId + ", Code: " + ErrorCode + " - " + Message;
            if (!Util.StringIsEmpty(AdvancedOrderRejectJson))
            {
                ret += (", AdvancedOrderRejectJson: " + AdvancedOrderRejectJson);
            }
            return ret;
        }
       
    }
}
