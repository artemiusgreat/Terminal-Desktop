//using Core.EnumSpace;
//using Core.ManagerSpace;
//using Core.ModelSpace;
//using ExchangeSharp;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Threading.Tasks;

//namespace Gateway.Alpaca
//{
//  /// <summary>
//  /// Implementation
//  /// </summary>
//  public class GatewayClient : GatewayModel, IGatewayModel
//  {
//    /// <summary>
//    /// Last quote
//    /// </summary>
//    protected IPointModel _point = null;

//    /// <summary>
//    /// HTTP client
//    /// </summary>
//    protected IClientService _serviceClient = null;

//    /// <summary>
//    /// Trade client
//    /// </summary>
//    protected ExchangeGeminiAPI _executionClient = null;

//    /// <summary>
//    /// API key
//    /// </summary>
//    public string Token { get; set; }

//    /// <summary>
//    /// API secret
//    /// </summary>
//    public string Secret { get; set; }

//    /// <summary>
//    /// Production or Sandbox
//    /// </summary>
//    public EnvironmentEnum Mode { get; set; } = EnvironmentEnum.Development;

//    /// <summary>
//    /// Establish connection with a server
//    /// </summary>
//    /// <param name="docHeader"></param>
//    public override Task Connect()
//    {
//      return Task.Run(async () =>
//      {
//        try
//        {
//          await Disconnect();

//          var subscription = OrderSenderStream.Subscribe(message =>
//          {
//            switch (message.Action)
//            {
//              case ActionEnum.Create: CreateOrders(message.Next); break;
//              case ActionEnum.Update: UpdateOrders(message.Next); break;
//              case ActionEnum.Delete: DeleteOrders(message.Next); break;
//            }
//          });

//          _disposables.Add(subscription);

//          switch (Mode)
//          {
//            case EnvironmentEnum.Production:

//              _dataClient = Environments.Live.GetAlpacaDataClient(new SecretKey(Token, Secret));
//              _executionClient = Environments.Live.GetAlpacaTradingClient(new SecretKey(Token, Secret));
//              _streamClient = Environments.Live.GetAlpacaDataStreamingClient(new SecretKey(Token, Secret));

//              break;

//            case EnvironmentEnum.Development:

//              _dataClient = Environments.Paper.GetAlpacaDataClient(new SecretKey(Token, Secret));
//              _executionClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(Token, Secret));
//              _streamClient = Environments.Paper.GetAlpacaDataStreamingClient(new SecretKey(Token, Secret));

//              break;
//          }

//          await GetAccountData();
//          await GetActiveOrders();
//          await GetActivePositions();
//          await Subscribe();
//        }
//        catch (Exception e)
//        {
//          InstanceManager<LogService>.Instance.Log.Error(e.ToString());
//        }
//      });
//    }

//    /// <summary>
//    /// Dispose environment
//    /// </summary>
//    /// <returns></returns>
//    public override Task Disconnect()
//    {
//      Unsubscribe();

//      _dataClient?.Dispose();
//      _streamClient?.Dispose();
//      _executionClient?.Dispose();
//      _serviceClient?.Client?.Dispose();
//      _disposables.ForEach(o => o.Dispose());
//      _disposables.Clear();

//      return Task.FromResult(0);
//    }

//    /// <summary>
//    /// Start streaming
//    /// </summary>
//    /// <returns></returns>
//    public override Task Subscribe()
//    {
//      _streamClient.ConnectAndAuthenticateAsync().ContinueWith(o =>
//      {
//        var streams = new List<IAlpacaDataSubscription>();

//        foreach (var instrument in Account.Instruments)
//        {
//          var dataStream = _streamClient.GetQuoteSubscription(instrument.Value.Name);
//          var executionStream = _streamClient.GetTradeSubscription(instrument.Value.Name);

//          streams.Add(dataStream);
//          streams.Add(executionStream);

//          _dataStreams.Add(dataStream);
//          _executionStreams.Add(executionStream);

//          dataStream.Received += OnInputQuote;
//          executionStream.Received += OnInputTrade;
//        }

//        _streamClient.Subscribe(streams);
//      });

//      return Task.FromResult(0);
//    }

//    /// <summary>
//    /// Stop streaming without but keep environment state
//    /// </summary>
//    /// <returns></returns>
//    public override Task Unsubscribe()
//    {
//      if (_streamClient != null)
//      {
//        var streams = new List<IAlpacaDataSubscription>();

//        _dataStreams.ForEach(o => o.Received -= OnInputQuote);
//        _executionStreams.ForEach(o => o.Received -= OnInputTrade);

//        _streamClient.Unsubscribe(streams.Concat(_dataStreams).Concat(_executionStreams));
//        _streamClient.DisconnectAsync();
//      }

//      return Task.FromResult(0);
//    }

//    /// <summary>
//    /// Place new order
//    /// </summary>
//    /// <param name="orders"></param>
//    /// <returns></returns>
//    public virtual Task<IEnumerable<ITransactionOrderModel>> CreateOrders(params ITransactionOrderModel[] orders)
//    {
//      return Task.Run(async () =>
//      {
//        foreach (var nextOrder in orders)
//        {
//          await CreateOrder(nextOrder);
//        }

//        return orders as IEnumerable<ITransactionOrderModel>;
//      });
//    }

//    /// <summary>
//    /// Update order
//    /// </summary>
//    /// <param name="orders"></param>
//    /// <returns></returns>
//    public virtual Task<IEnumerable<ITransactionOrderModel>> UpdateOrders(params ITransactionOrderModel[] orders)
//    {
//      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
//    }

//    /// <summary>
//    /// Cancel order
//    /// </summary>
//    /// <param name="orders"></param>
//    /// <returns></returns>
//    public virtual Task<IEnumerable<ITransactionOrderModel>> DeleteOrders(params ITransactionOrderModel[] orders)
//    {
//      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
//    }

//    /// <summary>
//    /// Convert abstract order to a real one
//    /// </summary>
//    /// <param name="internalOrder"></param>
//    /// <param name="orderClass"></param>
//    /// <returns></returns>
//    protected async Task<ITransactionOrderModel> CreateOrder(ITransactionOrderModel internalOrder, OrderClass? orderClass = null)
//    {
//      var size = ConversionManager.To<long>(internalOrder.Size);
//      var price = ConversionManager.To<decimal>(internalOrder.Price);
//      var span = MapInput.GetTimeSpan(internalOrder.TimeSpan.Value).Value;
//      var orderSide = MapInput.GetOrderSide(internalOrder.Side.Value).Value;
//      var orderType = MapInput.GetOrderType(internalOrder.Type.Value).Value;
//      var activationPrice = ConversionManager.To<decimal>(internalOrder.ActivationPrice);
//      var orderQuery = new NewOrderRequest(internalOrder.Instrument.Name, size, orderSide, orderType, span)
//      {
//        ClientOrderId = internalOrder.Id
//      };

//      if (orderClass != null)
//      {
//        orderQuery.OrderClass = orderClass;
//      }

//      switch (orderType)
//      {
//        case OrderType.Stop: orderQuery.StopPrice = price; break;
//        case OrderType.Limit: orderQuery.LimitPrice = price; break;
//        case OrderType.StopLimit: orderQuery.StopPrice = activationPrice; orderQuery.LimitPrice = price; break;
//      }

//      await _executionClient.PostOrderAsync(orderQuery);

//      foreach (var childData in internalOrder.Orders)
//      {
//        await CreateOrder(childData);
//      }

//      return internalOrder;
//    }

//    /// <summary>
//    /// Load account data
//    /// </summary>
//    /// <returns></returns>
//    protected async Task GetAccountData()
//    {
//      var account = await _executionClient.GetAccountAsync();

//      Account.Leverage = account.Multiplier;
//      Account.Currency = account.Currency.ToUpper();
//      Account.Balance = ConversionManager.To<double>(account.Equity);
//      Account.InitialBalance = ConversionManager.To<double>(account.LastEquity);
//    }

//    /// <summary>
//    /// Load orders
//    /// </summary>
//    /// <returns></returns>
//    protected async Task GetOrders()
//    {
//      var orders = await _executionClient.ListOrdersAsync(new ListOrdersRequest
//      {
//        LimitOrderNumber = 500,
//        OrderListSorting = SortDirection.Descending,
//        OrderStatusFilter = OrderStatusFilter.Closed
//      });

//      foreach (var o in orders)
//      {
//        var order = new TransactionOrderModel();

//        Account.Instruments.TryGetValue(o.Symbol, out IInstrumentModel instrument);

//        order.Size = o.Quantity;
//        order.Time = o.CreatedAtUtc;
//        order.Id = o.OrderId.ToString();
//        order.Type = MapOutput.GetOrderType(o.OrderType);
//        order.Side = MapOutput.GetOrderSide(o.OrderSide);
//        order.Status = MapOutput.GetOrderStatus(o.OrderStatus);
//        order.TimeSpan = MapOutput.GetTimeSpan(o.TimeInForce);
//        order.Price = ConversionManager.To<double>(o.AverageFillPrice ?? o.StopPrice ?? o.LimitPrice);
//        order.Instrument = instrument ?? new InstrumentModel
//        {
//          Name = o.Symbol
//        };

//        Account.Orders.Add(order);
//      }
//    }

//    /// <summary>
//    /// Load active orders
//    /// </summary>
//    /// <returns></returns>
//    protected async Task GetActiveOrders()
//    {
//      var orders = await _executionClient.ListOrdersAsync(new ListOrdersRequest
//      {
//        LimitOrderNumber = 500,
//        OrderListSorting = SortDirection.Descending,
//        OrderStatusFilter = OrderStatusFilter.Open
//      });

//      foreach (var o in orders)
//      {
//        var order = new TransactionOrderModel();

//        Account.Instruments.TryGetValue(o.Symbol, out IInstrumentModel instrument);

//        order.Size = o.Quantity;
//        order.Time = o.CreatedAtUtc;
//        order.Id = o.OrderId.ToString();
//        order.Type = MapOutput.GetOrderType(o.OrderType);
//        order.Side = MapOutput.GetOrderSide(o.OrderSide);
//        order.Status = MapOutput.GetOrderStatus(o.OrderStatus);
//        order.TimeSpan = MapOutput.GetTimeSpan(o.TimeInForce);
//        order.Price = ConversionManager.To<double>(o.AverageFillPrice ?? o.StopPrice ?? o.LimitPrice);
//        order.Instrument = instrument ?? new InstrumentModel
//        {
//          Name = o.Symbol
//        };

//        Account.ActiveOrders.Add(order);
//      }
//    }

//    /// <summary>
//    /// Load active positions
//    /// </summary>
//    /// <returns></returns>
//    protected async Task GetActivePositions()
//    {
//      var positions = await _executionClient.ListPositionsAsync();

//      foreach (var o in positions)
//      {
//        var position = new TransactionPositionModel();

//        Account.Instruments.TryGetValue(o.Symbol, out IInstrumentModel instrument);

//        position.Size = o.Quantity;
//        position.Time = DateTime.MinValue;
//        position.Type = OrderTypeEnum.Market;
//        position.Side = MapOutput.GetPositionSide(o.Side);
//        position.OpenPrice = ConversionManager.To<double>(o.AverageEntryPrice);
//        position.ClosePrice = ConversionManager.To<double>(o.AssetCurrentPrice);
//        position.GainLoss = ConversionManager.To<double>(o.UnrealizedProfitLoss);
//        position.GainLossPoints = ConversionManager.To<double>((o.AssetCurrentPrice - o.AverageEntryPrice)) * MapOutput.GetDirection(position.Side.Value);
//        position.Instrument = instrument ?? new InstrumentModel
//        {
//          Name = o.Symbol
//        };

//        position.OpenPrices = new List<ITransactionOrderModel>
//        {
//          new TransactionOrderModel
//          {
//            Price = position.OpenPrice,
//            Instrument = position.Instrument
//          }
//        };

//        Account.ActivePositions.Add(position);
//      }
//    }

//    /// <summary>
//    /// Process incoming quotes
//    /// </summary>
//    /// <param name="input"></param>
//    protected void OnInputQuote(IStreamQuote input)
//    {
//      var currentAsk = ConversionManager.To<double>(input.AskPrice);
//      var currentBid = ConversionManager.To<double>(input.BidPrice);
//      var previousAsk = _point?.Ask ?? currentAsk;
//      var previousBid = _point?.Bid ?? currentBid;

//      var point = new PointModel
//      {
//        Ask = currentAsk,
//        Bid = currentBid,
//        Time = input.TimeUtc,
//        AskSize = input.AskSize,
//        BidSize = input.BidSize,
//        Bar = new PointBarModel(),
//        Instrument = Account.Instruments[input.Symbol],
//        Last = ConversionManager.Compare(currentBid, previousBid) ? currentAsk : currentBid
//      };

//      _point = point;

//      UpdatePointProps(point, Account.Instruments[input.Symbol]);
//    }

//    /// <summary>
//    /// Process incoming quotes
//    /// </summary>
//    /// <param name="input"></param>
//    protected void OnInputTrade(IStreamTrade input)
//    {
//    }

//  }
//}
