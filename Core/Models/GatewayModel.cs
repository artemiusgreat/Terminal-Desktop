using Core.EnumSpace;
using Core.MessageSpace;
using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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
  }

  /// <summary>
  /// Interface that defines input and output processes
  /// </summary>
  public interface IGatewayModel : IStateModel, IDataModel, ITradeModel
  {
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public abstract class GatewayModel : StateModel, IGatewayModel
  {
    /// <summary>
    /// Validation rules
    /// </summary>
    private static TransactionOrderPriceValidation _orderRules = InstanceManager<TransactionOrderPriceValidation>.Instance;
    private static InstrumentCollectionsValidation _instrumentRules = InstanceManager<InstrumentCollectionsValidation>.Instance;

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
    /// <param name="instrument"></param>
    protected virtual IPointModel UpdatePointProps(IPointModel point, IInstrumentModel instrument)
    {
      point.Account = Account;
      point.Instrument = instrument;
      point.Name = point.Instrument.Name;
      point.ChartData = point.Instrument.ChartData;
      point.TimeFrame = point.Instrument.TimeFrame;

      UpdatePoints(point);

      var message = new TransactionMessage<IPointModel>
      {
        Action = ActionEnum.Create,
        Next = instrument.PointGroups.LastOrDefault()
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

    /// <summary>
    /// Update position properties based on specified order
    /// </summary>
    /// <param name="position"></param>
    /// <param name="order"></param>
    protected virtual ITransactionPositionModel UpdatePositionParams(ITransactionPositionModel position, ITransactionOrderModel order)
    {
      position.Id = order.Id;
      position.Name = order.Name;
      position.Description = order.Description;
      position.Type = order.Type;
      position.Size = order.Size;
      position.Group = order.Group;
      position.Price = order.Price;
      position.OpenPrice = order.Price;
      position.Instrument = order.Instrument;
      position.Orders = order.Orders;
      position.Time = order.Time;

      return position;
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
