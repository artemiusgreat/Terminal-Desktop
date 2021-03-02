using Core.EnumSpace;
using Core.ManagerSpace;
using Core.ModelSpace;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Websocket.Client;

namespace Gateway.Tradier
{
  /// <summary>
  /// Implementation
  /// </summary>
  public class GatewayClient : GatewayModel, IGatewayModel
  {
    /// <summary>
    /// Socket session ID
    /// </summary>
    protected string _streamSession = null;

    /// <summary>
    /// API key
    /// </summary>
    public string Token
    {
      get
      {
        switch (Mode)
        {
          case EnvironmentEnum.Live: return LiveToken;
          case EnvironmentEnum.Sandbox: return SnadboxToken;
        }

        return null;
      }
    }

    /// <summary>
    /// HTTP endpoint
    /// </summary>
    public string Source
    {
      get
      {
        switch (Mode)
        {
          case EnvironmentEnum.Live: return LiveSource;
          case EnvironmentEnum.Sandbox: return SandboxSource;
        }

        return null;
      }
    }

    /// <summary>
    /// API key
    /// </summary>
    public string LiveToken { get; set; }

    /// <summary>
    /// Sandbox API key
    /// </summary>
    public string SnadboxToken { get; set; }

    /// <summary>
    /// HTTP endpoint
    /// </summary>
    public string LiveSource { get; set; } = "https://api.tradier.com/v1";

    /// <summary>
    /// Sandbox HTTP endpoint
    /// </summary>
    public string SandboxSource { get; set; } = "https://sandbox.tradier.com/v1";

    /// <summary>
    /// Socket endpoint
    /// </summary>
    public string StreamSource { get; set; } = "wss://ws.tradier.com/v1";

    /// <summary>
    /// Establish connection with a server
    /// </summary>
    /// <param name="docHeader"></param>
    public override Task Connect()
    {
      return Task.Run(async () =>
      {
        try
        {
          await Disconnect();

          _serviceClient = new ClientService();
          _serviceClient.Client.DefaultRequestHeaders.Add("Accept", "application/json");
          _serviceClient.Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);

          _connections.Add(_serviceClient);

          //await GetAccountData();
          //await GetActiveOrders();
          //await GetActivePositions();
          await Subscribe();
        }
        catch (Exception e)
        {
          InstanceManager<LogService>.Instance.Log.Error(e.ToString());
        }
      });
    }

    /// <summary>
    /// Disconnect
    /// </summary>
    /// <returns></returns>
    public override Task Disconnect()
    {
      Unsubscribe();

      _connections.ForEach(o => o.Dispose());
      _connections.Clear();

      return Task.FromResult(0);
    }

    /// <summary>
    /// Start streaming
    /// </summary>
    /// <returns></returns>
    public override async Task Subscribe()
    {
      await Unsubscribe();

      // Orders

      var orderSubscription = OrderSenderStream.Subscribe(message =>
      {
        switch (message.Action)
        {
          case ActionEnum.Create: CreateOrders(message.Next); break;
          case ActionEnum.Update: UpdateOrders(message.Next); break;
          case ActionEnum.Delete: DeleteOrders(message.Next); break;
        }
      });

      _subscriptions.Add(orderSubscription);

      // Streaming

      _streamSession = await GetStreamSession();

      var client = new WebsocketClient(new Uri(StreamSource + "/markets/events"), _streamOptions)
      {
        Name = Account.Name,
        ReconnectTimeout = TimeSpan.FromSeconds(30),
        ErrorReconnectTimeout = TimeSpan.FromSeconds(30)
      };

      var connectionSubscription = client.ReconnectionHappened.Subscribe(message => { });
      var disconnectionSubscription = client.DisconnectionHappened.Subscribe(message => { });
      var messageSubscription = client.MessageReceived.Subscribe(message =>
      {
        dynamic input = JObject.Parse(message.Text);

        var inputStream = $"{ input.type }";

        switch (inputStream)
        {
          case "quote":

            OnInputQuote(input);
            break;

          case "trade": break;
          case "tradex": break;
          case "summary": break;
          case "timesale": break;
        }
      });

      _subscriptions.Add(messageSubscription);
      _subscriptions.Add(connectionSubscription);
      _subscriptions.Add(disconnectionSubscription);

      await client.Start();

      var query = new
      {
        linebreak = true,
        sessionid = _streamSession,
        symbols = Account.Instruments.Values.Select(o => o.Name)
      };

      client.Send(ConversionManager.Serialize(query));
    }

    public override Task Unsubscribe()
    {
      _subscriptions.ForEach(o => o.Dispose());
      _subscriptions.Clear();

      return Task.FromResult(0);
    }

    public override Task<IEnumerable<ITransactionOrderModel>> CreateOrders(params ITransactionOrderModel[] orders)
    {
      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
    }

    public override Task<IEnumerable<ITransactionOrderModel>> UpdateOrders(params ITransactionOrderModel[] orders)
    {
      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
    }

    public override Task<IEnumerable<ITransactionOrderModel>> DeleteOrders(params ITransactionOrderModel[] orders)
    {
      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
    }

    /// <summary>
    /// Process incoming quotes
    /// </summary>
    /// <param name="input"></param>
    protected void OnInputQuote(dynamic input)
    {
      var dateAsk = ConversionManager.To<long>(input.askdate);
      var dateBid = ConversionManager.To<long>(input.biddate);
      var currentAsk = ConversionManager.To<double>(input.ask);
      var currentBid = ConversionManager.To<double>(input.bid);
      var previousAsk = _point?.Ask ?? currentAsk;
      var previousBid = _point?.Bid ?? currentBid;
      var symbol = $"{ input.symbol }";

      var point = new PointModel
      {
        Ask = currentAsk,
        Bid = currentBid,
        Bar = new PointBarModel(),
        Instrument = Account.Instruments[symbol],
        AskSize = ConversionManager.To<double>(input.asksz),
        BidSize = ConversionManager.To<double>(input.bidsz),
        Time = DateTimeOffset.FromUnixTimeMilliseconds(Math.Max(dateAsk, dateBid)).DateTime,
        Last = ConversionManager.Compare(currentBid, previousBid) ? currentAsk : currentBid
      };

      _point = point;

      UpdatePointProps(point);
    }

    /// <summary>
    /// Process incoming quotes
    /// </summary>
    /// <param name="input"></param>
    protected void OnInputTrade(dynamic input)
    {
    }

    /// <summary>
    /// Create session to start streaming
    /// </summary>
    /// <returns></returns>
    protected async Task<string> GetStreamSession()
    {
      using (var sessionClient = new ClientService())
      {
        sessionClient.Client.DefaultRequestHeaders.Add("Accept", "application/json");
        sessionClient.Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + LiveToken);

        dynamic session = await sessionClient.Post<dynamic>(LiveSource + "/markets/events/session");

        return $"{ session.stream.sessionid }";
      }
    }
  }
}
