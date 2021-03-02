using Core.EnumSpace;
using Core.ManagerSpace;
using Core.ModelSpace;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Gateway.Oanda
{
  /// <summary>
  /// Implementation
  /// </summary>
  public class GatewayClient : GatewayModel, IGatewayModel
  {
    /// <summary>
    /// API key
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// HTTP endpoint
    /// </summary>
    public string Source { get; set; } = "https://api-fxpractice.oanda.com";

    /// <summary>
    /// Socket endpoint
    /// </summary>
    public string StreamSource { get; set; } = "https://stream-fxpractice.oanda.com";

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

          _connections.Add(_serviceClient ??= new ClientService());

          //await GetPositions();
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

      // Streaming

      var stream = await GetStream();
      var span = TimeSpan.FromMilliseconds(0);
      var scheduler = InstanceManager<ScheduleService>.Instance.Scheduler;
      var reader = new StreamReader(stream);
      var process = Observable
        .Interval(span, scheduler)
        .Take(1)
        .Subscribe(async o =>
        {
          while (_subscriptions.Any())
          {
            var message = await reader.ReadLineAsync();

            if (message.IndexOf("HEARTBEAT", StringComparison.OrdinalIgnoreCase) < 0)
            {
              OnInputData(ConversionManager.Deserialize<InputPoint>(message));
            }
          }
        });

      _subscriptions.Add(reader);
      _subscriptions.Add(stream);

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
    protected void OnInputData(InputPoint input)
    {
      var currentAsk = input.Ask;
      var currentBid = input.Bid;
      var previousAsk = _point?.Ask ?? currentAsk;
      var previousBid = _point?.Bid ?? currentBid;
      var instrument = $"{ input.Instrument }";

      var point = new PointModel
      {
        Ask = currentAsk,
        Bid = currentBid,
        Time = input.Time,
        Bar = new PointBarModel(),
        Instrument = Account.Instruments[instrument],
        Last = ConversionManager.Compare(currentBid, previousBid) ? currentAsk : currentBid
      };

      if (input.Asks.Any())
      {
        var edge = input.Asks.Min(o => new { o.Price, o.Size });

        point.Ask = Math.Min(point.Ask.Value, edge.Price);
        point.AskSize = edge.Size;
      }

      if (input.Bids.Any())
      {
        var edge = input.Bids.Max(o => new { o.Price, o.Size });

        point.Bid = Math.Max(point.Bid.Value, edge.Price);
        point.BidSize = edge.Size;
      }

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
    /// Get positions
    /// </summary>
    protected async Task<IList<ITransactionPositionModel>> GetPositions()
    {
      var inputs = new
      {
        request = "/v1/mytrades",
        symbol = "btcusd",
        nonce = DateTime.Now.Ticks
      };

      return null; // MapInput.Positions(await Query(inputs));
    }

    /// <summary>
    /// Create streaming URL
    /// </summary>
    /// <returns></returns>
    protected async Task<Stream> GetStream()
    {
      var query = new Dictionary<dynamic, dynamic>
      {
        ["instruments"] = string.Join(",", Account.Instruments.Select(o => o.Value.Name))
      };

      var headers = new Dictionary<dynamic, dynamic>
      {
        ["Authorization"] = $"Bearer { Token }"
      };

      return await _serviceClient.Stream($"{ StreamSource }/v3/accounts/{ Account.Id }/pricing/stream", query, headers);
    }

    /// <summary>
    /// Send query
    /// </summary>
    protected async Task<dynamic> Query(dynamic inputs)
    {
      var query = Convert.ToBase64String(ConversionManager.Bytes(ConversionManager.Serialize(inputs)));
      var queryHeaders = new Dictionary<dynamic, dynamic>()
      {
        ["Cache-Control"] = "no-cache",
        ["Accept"] = "application/json",
        ["X-GEMINI-APIKEY"] = Token,
        ["X-GEMINI-PAYLOAD"] = query
      };

      return ConversionManager.Deserialize<dynamic>(await _serviceClient.Post(Source + inputs.request, null, queryHeaders));
    }
  }
}
