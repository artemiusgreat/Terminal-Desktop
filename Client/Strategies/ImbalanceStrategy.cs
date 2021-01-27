using Core.CollectionSpace;
using Core.EnumSpace;
using Core.IndicatorSpace;
using Core.MessageSpace;
using Core.ModelSpace;
using Gateway.Simulation;
using System;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Client.StrategySpace
{
  public class ImbalanceStrategy : BaseStrategy
  {
    const string _asset = "GOOG";
    const string _account = "Simulation";

    protected DateTime? _date = null;
    protected TimeSpan? _span = null;
    protected IInstrumentModel _instrument = null;
    protected ImbalanceIndicator _imbalanceIndicator = null;
    protected PerformanceIndicator _performanceIndicator = null;

    public override Task OnLoad()
    {
      _date = DateTime.MinValue;
      _span = TimeSpan.FromMinutes(1);

      _instrument = new InstrumentModel
      {
        Name = _asset,
        TimeFrame = _span
      };

      var account = new AccountModel
      {
        Name = _account,
        Balance = 50000,
        InitialBalance = 50000,
        Instruments = new NameCollection<string, IInstrumentModel> { [_asset] = _instrument }
      };

      var gateway = new GatewayClient
      {
        Name = _account,
        Account = account,
        Evaluate = Parse,
        Source = ConfigurationManager.AppSettings["DataLocation"].ToString()
      };

      _imbalanceIndicator = new ImbalanceIndicator { Name = "Imbalance" };
      _performanceIndicator = new PerformanceIndicator { Name = "Balance" };

      _disposables.Add(gateway
        .Account
        .Instruments
        .Values
        .Select(o => o.PointGroups.ItemStream)
        .Merge()
        .Subscribe(OnData));

      CreateCharts();
      CreateGateways(gateway);

      return Task.FromResult(0);
    }

    protected void OnData(ITransactionMessage<IPointModel> message)
    {
      var point = message.Next;
      var account = point.Account;
      var instrument = point.Account.Instruments[_asset];
      var points = instrument.PointGroups;
      var volumes = _imbalanceIndicator.Calculate(points).Values;
      var performanceIndicator = _performanceIndicator.Calculate(points, Gateways.Select(o => o.Account));

      if (points.Count > 1 && IsNextPoint(point))
      {
        var currentPoint = points.ElementAt(points.Count - 1);
        var previousPoint = points.ElementAt(points.Count - 2);
        var currentVolume = volumes.ElementAt(volumes.Count - 1).Last;
        var previousVolume = volumes.ElementAt(volumes.Count - 2).Last;
        var isLong = currentVolume > 0 && previousVolume > 0 && previousPoint.Bar.Close > previousPoint.Bar.Open;
        var isShort = currentVolume < 0 && previousVolume < 0 && previousPoint.Bar.Close < previousPoint.Bar.Open;

        if (account.ActiveOrders.Count == 0 && account.ActivePositions.Count == 0)
        {
          if (isLong) CreateOrder(point, TransactionTypeEnum.Buy, 1);
          if (isShort) CreateOrder(point, TransactionTypeEnum.Sell, 1);
        }

        //if (account.ActivePositions.Count > 0)
        //{
        //  var activePosition = account.ActivePositions.Last();

        //  switch (activePosition.Type)
        //  {
        //    case TransactionTypeEnum.Buy: if (isShort) CreateOrder(point, TransactionTypeEnum.Sell, activePosition.Size.Value + 1); break;
        //    case TransactionTypeEnum.Sell: if (isLong) CreateOrder(point, TransactionTypeEnum.Buy, activePosition.Size.Value + 1); break;
        //  }
        //}
      }
    }

    /// <summary>
    /// Next bar event
    /// </summary>
    /// <param name="pointModel"></param>
    /// <returns></returns>
    protected bool IsNextPoint(IPointModel pointModel)
    {
      if (Equals(pointModel.Time, _date) == false)
      {
        _date = pointModel.Time;

        return true;
      }

      return false;
    }

    /// <summary>
    /// Helper method to send orders
    /// </summary>
    /// <param name="point"></param>
    /// <param name="side"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    protected ITransactionOrderModel CreateOrder(IPointModel point, TransactionTypeEnum side, double size)
    {
      var gateway = point.Account.Gateway;
      var instrument = point.Account.Instruments[_asset];
      var order = new TransactionOrderModel
      {
        Size = size,
        Type = side,
        Instrument = instrument
      };

      switch (side)
      {
        case TransactionTypeEnum.Buy:

          order.Orders.Add(new TransactionOrderModel
          {
            Size = size,
            Type = TransactionTypeEnum.SellLimit,
            Price = point.Ask + 2,
            Instrument = instrument,
            Container = order
          });

          order.Orders.Add(new TransactionOrderModel
          {
            Size = size,
            Type = TransactionTypeEnum.SellStop,
            Price = point.Bid - 2,
            Instrument = instrument,
            Container = order
          });

          break;

        case TransactionTypeEnum.Sell:

          order.Orders.Add(new TransactionOrderModel
          {
            Size = size,
            Type = TransactionTypeEnum.SellStop,
            Price = point.Bid + 2,
            Instrument = instrument,
            Container = order
          });

          order.Orders.Add(new TransactionOrderModel
          {
            Size = size,
            Type = TransactionTypeEnum.BuyLimit,
            Price = point.Ask - 2,
            Instrument = instrument,
            Container = order
          });

          break;
      }

      gateway.OrderSenderStream.OnNext(new TransactionMessage<ITransactionOrderModel>
      {
        Action = ActionEnum.Create,
        Next = order
      });

      _date = point.Time;

      return order;
    }

    /// <summary>
    /// Define what gateways will be used
    /// </summary>
    protected void CreateGateways(IGatewayModel gateway)
    {
      Gateways.Add(gateway);
    }

    /// <summary>
    /// Define what entites will be displayed on the chart
    /// </summary>
    protected void CreateCharts()
    {
      var dealIndicator = new ChartModel
      {
        Name = "Transactions",
        Area = _asset,
        Shape = nameof(ShapeEnum.Arrow)
      };

      _instrument.Chart.Name = _asset;
      _instrument.Chart.Area = _asset;
      _instrument.Chart.Shape = nameof(ShapeEnum.Candle);

      _imbalanceIndicator.Chart.Center = 0;
      _imbalanceIndicator.Chart.Name = _imbalanceIndicator.Name;
      _imbalanceIndicator.Chart.Area = "Imbalance";
      _imbalanceIndicator.Chart.Shape = nameof(ShapeEnum.Bar);

      _performanceIndicator.Chart.Name = _performanceIndicator.Name;
      _performanceIndicator.Chart.Area = "Performance";
      _performanceIndicator.Chart.Shape = nameof(ShapeEnum.Area);

      Charts.Add(_instrument.Chart);
      Charts.Add(_imbalanceIndicator.Chart);
      Charts.Add(_performanceIndicator.Chart);
      Charts.Add(dealIndicator);
    }
  }
}