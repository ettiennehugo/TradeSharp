using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradeSharp.Data;
using TradeSharp.PolygonIO.Messages;
using System.Text;
using System.Net.WebSockets;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.PolygonIO
{
  /// <summary>
  /// HTTP client to work with the Polygon IO end-points.
  /// </summary>
  public class Client
  {
    //constants
    public const string BaseUrl = "https://api.polygon.io";
    public const string RealTimeUrl = "wss://socket.polygon.io/stocks";
    public const string DelayedUrl = "wss://delayed.polygon.io/stocks";
    public const int SubscriptionApiBufferSize = 4096;    //buffer size for the subscription API result messages

    //enums
    public enum AssetClass
    {
      Stocks,
      Options,
      Forex,
      Fx
    }

    //types


    //attributes
    protected static Client s_instance;
    protected ILogger m_logger;
    protected string m_apiKey;
    protected int m_requestLimit;
    protected HttpClient m_httpClient;

    //properties


    //constructors
    public static Client GetInstance(ILogger logger, string apiKey, int requestLimit)
    {
      if (s_instance == null)
        s_instance = new Client(logger, apiKey, requestLimit);
      return s_instance;
    }

    private Client(ILogger logger,string apiKey, int requestLimit)
    {
      m_logger = logger;
      m_apiKey = apiKey;
      m_requestLimit = requestLimit;
      m_httpClient = new HttpClient();
    }

    //finalizers


    //interface implementations


    //methods
    public string GetExchangesUri() => $"{BaseUrl}/v3/reference/exchanges?apiKey={m_apiKey}";
    public string GetTickersUri(string ticker) => $"{BaseUrl}/v3/reference/tickers?apiKey={m_apiKey}";
    public string GetTickerDetailsUri(string ticker) => $"{BaseUrl}/v3/reference/tickers/{ticker}?apiKey={m_apiKey}";
    public string GetStockAggregatesUri(string ticker, Resolution resolution, long startDate, long endDate, int multiplier = 1) => $"{BaseUrl}/v2/aggs/ticker/{ticker}/range/{multiplier}/{resolution.ToString().ToLower()}/{startDate}/{endDate}?adjusted=true&sort=asc&limit={m_requestLimit}&apiKey={m_apiKey}";
    public string GetTradesUri(string ticker, DateTime startDate, DateTime endDate) => $"{BaseUrl}/v3/trades/{ticker}?limit={m_requestLimit}&timestamp.gte={startDate.ToUniversalTime().ToString("o")}&timestamp.lte={endDate.ToUniversalTime().ToString("o")}&sort=timestamp&apiKey={m_apiKey}";
    public string GetSubscriptionUri(bool realTime = true) => realTime ? $"{RealTimeUrl}?apiKey={m_apiKey}" : $"{DelayedUrl}?apiKey={m_apiKey}";
    public string GetQuotesUri(string ticker, DateTime startDate, DateTime endDate) => $"{BaseUrl}/v3/quotes/{ticker}?limit={m_requestLimit}&timestamp.gte={startDate.ToUniversalTime().ToString("o")}&timestamp.lte={endDate.ToUniversalTime().ToString("o")}&sort=timestamp&apiKey={m_apiKey}";
    public string GetConditionsUri(AssetClass assetClass) => $"{BaseUrl}/v3/reference/conditions?asset_class={assetClass.ToString().ToLower()}&limit=1000&apiKey={m_apiKey}";

    public async Task<IList<Messages.ExchangeDto>> GetExchanges(IProgressDialog? progressDialog)
    {
      var exchanges = new List<Messages.ExchangeDto>();
      var uri = GetExchangesUri();
      var exchangeResult = await GetPolygonApi<ExchangeResponseDto>(uri, progressDialog);
      if (exchangeResult?.Results is not null)
        exchanges.AddRange(exchangeResult.Results);

      return exchanges;
    }

    public async Task<IList<Messages.TickersDto>> GetTickers(IProgressDialog? progressDialog)
    {
      var tickers = new List<Messages.TickersDto>();
      var uri = GetTickersUri("");
      var tickerResult = await GetPolygonApi<TickersResponseDto>(uri, progressDialog);
      int tickerCount = 0;
      if (tickerResult?.Results is not null)
      {
        tickers.AddRange(tickerResult.Results);
        progressDialog?.LogInformation($"Received {tickerCount} to {tickerCount + tickerResult.Results.Count - 1} tickers");
        tickerCount += tickerResult.Results.Count;
      }

      while (tickerResult?.NextUrl != null)
      {
        tickerResult = await GetPolygonApi<TickersResponseDto>(tickerResult.NextUrl + "&apiKey=" + m_apiKey, progressDialog);
        tickers.AddRange(tickerResult?.Results);
        progressDialog?.LogInformation($"Received {tickerCount} to {tickerCount + tickerResult.Results.Count} tickers");
        tickerCount += tickerResult.Results.Count;
      }

      return tickers;
    }

    public async Task<Messages.TickerDetailsResponseDto> GetTickerDetails(string ticker, IProgressDialog? progressDialog)
    {
      var uri = GetTickerDetailsUri(ticker);
      return await GetPolygonApi<Messages.TickerDetailsResponseDto>(uri, progressDialog);
    }

    public async Task<IList<Messages.BarDataDto>> GetHistoricalData(string ticker, Resolution resolution, DateTime startDate, DateTime endDate, IProgressDialog? progressDialog)
    {
      var allStockAggregates = new List<Messages.BarDataDto>();
      var startDateUnix = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();
      var endDateUnix = new DateTimeOffset(endDate).ToUnixTimeMilliseconds(); // TODO: Second resolution can be 1 too many
      var uri = GetStockAggregatesUri(ticker, resolution, startDateUnix, endDateUnix);
      var aggregateResult = await GetPolygonApi<BarDataResponseDto>(uri, progressDialog);
      if (aggregateResult?.Results is not null)
        allStockAggregates.AddRange(aggregateResult.Results);

      while (aggregateResult?.NextUrl != null)
      {
        aggregateResult = await GetPolygonApi<BarDataResponseDto>(aggregateResult.NextUrl + "&apiKey=" + m_apiKey, progressDialog);
        allStockAggregates.AddRange(aggregateResult!.Results);
      }

      return allStockAggregates;
    }

    /// <summary>
    /// Subscribe to 1-minute data updated in real-time/delyed fashion - ticker == "*" would subscribe to all updates from the market.
    /// </summary>
    public Action<Messages.BarDataM1ResultDto> BarDataM1Handler;
    public async Task SubscribeToM1(string ticker, bool realTime, CancellationToken cancellationToken)
    {
      using (var socket = new ClientWebSocket())
      {
        var uri = GetSubscriptionUri(realTime);
        await socket.ConnectAsync(new Uri(uri), cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"auth\",\"params\":\"" + m_apiKey + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"subscribe\",\"params\":\"AM." + ticker + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await AwaitSubscriptionResponse(socket, cancellationToken, BarDataM1Handler);
      }
    }

    /// <summary>
    /// Subscribe to 1-second data updated in real-time/delyed fashion - ticker == "*" would subscribe to all updates from the market.
    /// </summary>
    public Action<Messages.BarDataS1ResultDto> BarDataS1Handler;
    public async Task SubscribeToS1(string ticker, bool realTime, CancellationToken cancellationToken)
    {
      using (var socket = new ClientWebSocket())
      {
        var uri = GetSubscriptionUri(realTime);
        await socket.ConnectAsync(new Uri(uri), cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"auth\",\"params\":\"" + m_apiKey + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"subscribe\",\"params\":\"A." + ticker + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await AwaitSubscriptionResponse(socket, cancellationToken, BarDataS1Handler);
      }
    }

    public Action<Messages.StockTradesResultDto> StockTradesHandler;
    public async Task SubscribeToTrades(string ticker, bool realTime, CancellationToken cancellationToken)
    {
      using (var socket = new ClientWebSocket())
      {
        var uri = GetSubscriptionUri(realTime);
        await socket.ConnectAsync(new Uri(uri), cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"auth\",\"params\":\"" + m_apiKey + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"subscribe\",\"params\":\"T." + ticker + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await AwaitSubscriptionResponse(socket, cancellationToken, StockTradesHandler);
      }
    }

    public Action<Messages.StockQuotesResultDto> StockQuotesHandler;
    public async Task SubscribeToQuotes(string ticker, bool realTime, CancellationToken cancellationToken)
    {
      using (var socket = new ClientWebSocket())
      {
        var uri = GetSubscriptionUri(realTime);
        await socket.ConnectAsync(new Uri(uri), cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"auth\",\"params\":\"" + m_apiKey + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"action\":\"subscribe\",\"params\":\"Q." + ticker + "\"}"), WebSocketMessageType.Text, true, cancellationToken);
        await AwaitSubscriptionResponse(socket, cancellationToken, StockQuotesHandler);
      }
    }

    private async Task<T?> GetPolygonApi<T>(string url, IProgressDialog? progressDialog)
    {
      var responseMessage = await m_httpClient.GetAsync(url);
      if (!responseMessage.IsSuccessStatusCode)
      {
        m_logger.LogError($"Error getting data from {url}: {responseMessage.ReasonPhrase}");
        progressDialog?.LogError($"Error getting data from {url}: {responseMessage.ReasonPhrase}");
        return default;
      }

      var response = await responseMessage.Content.ReadAsStringAsync();
      var options = new JsonSerializerOptions { AllowTrailingCommas = true };
      var data = JsonSerializer.Deserialize<T>(response, options);
      return data;
    }

    private async Task AwaitSubscriptionResponse<T>(WebSocket socket, CancellationToken cancellationToken, Action<T> handler)
    {
      var buffer = new byte[SubscriptionApiBufferSize]; //response buffer size, in most cases this would be more than enough
      while (socket.State == WebSocketState.Open)
      {
        WebSocketReceiveResult result;
        var messageBuffer = new MemoryStream();
        do
        {
          result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
          //write the received chunk to the messageBuffer
          messageBuffer.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage); //keep reading until the end of the message

        messageBuffer.Seek(0, SeekOrigin.Begin); //reset the position of the messageBuffer to read from the beginning

        if (result.MessageType == WebSocketMessageType.Text)
        {
          using (var reader = new StreamReader(messageBuffer, Encoding.UTF8))
          {
            var messageString = await reader.ReadToEndAsync();
            var quotes = JsonSerializer.Deserialize<T>(messageString);
            if (quotes != null) handler?.Invoke(quotes);
          }
        }
        else if (result.MessageType == WebSocketMessageType.Close)
          await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
      }
    }
  }
}
