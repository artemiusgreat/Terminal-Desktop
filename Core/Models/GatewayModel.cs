using Core.EnumSpace;
using Core.MessageSpace;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Websocket.Client;

namespace Core.ModelSpace
{
  /// <summary>
  /// Generic market data gateway
  /// </summary>
  public interface IDataModel
  {
    /// <summary>
    /// Reference to the account
    /// </summary>
    IAccountModel Account { get; set; }

    /// <summary>
    /// Incoming data event
    /// </summary>
    ISubject<ITransactionMessage<IPointModel>> DataStream { get; }
  }

  /// <summary>
  /// Generic trading gateway
  /// </summary>
  public interface ITradeModel
  {
    /// <summary>
    /// Send order event
    /// </summary>
    ISubject<ITransactionMessage<ITransactionOrderModel>> OrderSenderStream { get; }

    /// <summary>
    /// Create orders
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    Task<IEnumerable<ITransactionOrderModel>> CreateOrders(params ITransactionOrderModel[] orders);

    /// <summary>
    /// Update orders
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    Task<IEnumerable<ITransactionOrderModel>> UpdateOrders(params ITransactionOrderModel[] orders);

    /// <summary>
    /// Close or cancel orders
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    Task<IEnumerable<ITransactionOrderModel>> DeleteOrders(params ITransactionOrderModel[] orders);
  }

  /// <summary>
  /// Interface that defines input and output processes
  /// </summary>
  public interface IGatewayModel : IStateModel, IDataModel, ITradeModel
  {
    /// <summary>
    /// Production or Development mode
    /// </summary>
    EnvironmentEnum Mode { get; set; }
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public abstract class GatewayModel : StateModel, IGatewayModel
  {
    /// <summary>
    /// Last quote
    /// </summary>
    protected IPointModel _point = null;

    /// <summary>
    /// HTTP client
    /// </summary>
    protected IClientService _serviceClient = null;

    /// <summary>
    /// Instance of the streaming client
    /// </summary>
    protected IWebsocketClient _streamClient = null;

    /// <summary>
    /// Unix time
    /// </summary>
    protected DateTime _unixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Socket connection options
    /// </summary>
    protected Func<ClientWebSocket> _streamOptions = new Func<ClientWebSocket>(() =>
    {
      return new ClientWebSocket
      {
        Options = { KeepAliveInterval = TimeSpan.FromSeconds(30) }
      };
    });

    /// <summary>
    /// Validation rules
    /// </summary>
    protected static TransactionOrderPriceValidation _orderRules = InstanceManager<TransactionOrderPriceValidation>.Instance;
    protected static InstrumentCollectionsValidation _instrumentRules = InstanceManager<InstrumentCollectionsValidation>.Instance;

    /// <summary>
    /// Production or Sandbox
    /// </summary>
    public EnvironmentEnum Mode { get; set; } = EnvironmentEnum.Sandbox;

    /// <summary>
    /// Reference to the account
    /// </summary>
    public virtual IAccountModel Account { get; set; }

    /// <summary>
    /// Incoming data event
    /// </summary>
    public virtual ISubject<ITransactionMessage<IPointModel>> DataStream { get; }

    /// <summary>
    /// Send order event
    /// </summary>
    public virtual ISubject<ITransactionMessage<ITransactionOrderModel>> OrderSenderStream { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    public GatewayModel()
    {
      DataStream = new Subject<ITransactionMessage<IPointModel>>();
      OrderSenderStream = new Subject<ITransactionMessage<ITransactionOrderModel>>();
    }

    /// <summary>
    /// Create orders
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    public virtual Task<IEnumerable<ITransactionOrderModel>> CreateOrders(params ITransactionOrderModel[] orders)
    {
      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
    }

    /// <summary>
    /// Update orders
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    public virtual Task<IEnumerable<ITransactionOrderModel>> UpdateOrders(params ITransactionOrderModel[] orders)
    {
      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
    }

    /// <summary>
    /// Close or cancel orders
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    public virtual Task<IEnumerable<ITransactionOrderModel>> DeleteOrders(params ITransactionOrderModel[] orders)
    {
      return Task.FromResult(orders as IEnumerable<ITransactionOrderModel>);
    }

    /// <summary>
    /// Ensure that each series has a name and can be attached to specific area on the chart
    /// </summary>
    /// <param name="model"></param>
    protected bool EnsureOrderProps(params ITransactionOrderModel[] models)
    {
      var errors = new List<ValidationFailure>();

      foreach (var model in models)
      {
        errors.AddRange(_orderRules.Validate(model).Errors);
        errors.AddRange(_instrumentRules.Validate(model.Instrument).Errors);
        errors.AddRange(model.Orders.SelectMany(o => _orderRules.Validate(o).Errors));
        errors.AddRange(model.Orders.SelectMany(o => _instrumentRules.Validate(o.Instrument).Errors));
      }

      foreach (var error in errors)
      {
        InstanceManager<LogService>.Instance.Log.Error(error.ErrorMessage);
      }

      return errors.Any() == false;
    }

    /// <summary>
    /// Update missing values of a data point
    /// </summary>
    /// <param name="point"></param>
    protected virtual IPointModel UpdatePointProps(IPointModel point)
    {
      point.Account = Account;
      point.Name = point.Instrument.Name;
      point.ChartData = point.Instrument.ChartData;
      point.TimeFrame = point.Instrument.TimeFrame;

      UpdatePoints(point);

      var message = new TransactionMessage<IPointModel>
      {
        Action = ActionEnum.Create,
        Next = point.Instrument.PointGroups.LastOrDefault()
      };

      DataStream.OnNext(message);

      return point;
    }

    /// <summary>
    /// Update collection with points
    /// </summary>
    /// <param name="point"></param>
    protected virtual IPointModel UpdatePoints(IPointModel point)
    {
      point.Instrument.Points.Add(point);
      point.Instrument.PointGroups.Add(point);

      return point;
    }
  }

  /// <summary>
  /// Validation rules
  /// </summary>
  public class GatewayValidation : AbstractValidator<IGatewayModel>
  {
    public GatewayValidation()
    {
      RuleFor(o => o.Name).NotNull().NotEmpty().WithMessage("No name");
    }
  }
}
