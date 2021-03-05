using Core.EnumSpace;
using Core.MessageSpace;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;

namespace Core.ModelSpace
{
  /// <summary>
  /// Generic order model
  /// </summary>
  public interface ITransactionOrderModel : ITransactionModel
  {
    /// <summary>
    /// Time when order was actually executed
    /// </summary>
    DateTime? DealTime { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    OrderTypeEnum? Type { get; set; }

    /// <summary>
    /// Side
    /// </summary>
    OrderSideEnum? Side { get; set; }

    /// <summary>
    /// Time in force
    /// </summary>
    OrderTimeSpanEnum? TimeSpan { get; set; }

    /// <summary>
    /// Reference to the main order in the hierarchy
    /// </summary>
    ITransactionOrderModel Container { get; set; }

    /// <summary>
    /// List of related orders in the hierarchy
    /// </summary>
    IList<ITransactionOrderModel> Orders { get; set; }

    /// <summary>
    /// Order events
    /// </summary>
    ISubject<ITransactionMessage<ITransactionOrderModel>> OrderStream { get; set; }
  }

  /// <summary>
  /// Generic order model
  /// </summary>
  public class TransactionOrderModel : TransactionModel, ITransactionOrderModel
  {
    /// <summary>
    /// Time when order was actually executed
    /// </summary>
    public virtual DateTime? DealTime { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public virtual OrderTypeEnum? Type { get; set; }

    /// <summary>
    /// Side
    /// </summary>
    public virtual OrderSideEnum? Side { get; set; }

    /// <summary>
    /// Time in force
    /// </summary>
    public virtual OrderTimeSpanEnum? TimeSpan { get; set; }

    /// <summary>
    /// Reference to the main order in the hierarchy
    /// </summary>
    public virtual ITransactionOrderModel Container { get; set; }

    /// <summary>
    /// List of related orders in the hierarchy
    /// </summary>
    public virtual IList<ITransactionOrderModel> Orders { get; set; }

    /// <summary>
    /// Order events
    /// </summary>
    public virtual ISubject<ITransactionMessage<ITransactionOrderModel>> OrderStream { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public TransactionOrderModel()
    {
      Id = Guid.NewGuid().ToString("N");
      Orders = new List<ITransactionOrderModel>();
      OrderStream = new Subject<ITransactionMessage<ITransactionOrderModel>>();
    }
  }

  /// <summary>
  /// Validation rules
  /// </summary>
  public class TransactionOrderValidation : AbstractValidator<ITransactionOrderModel>
  {
    public TransactionOrderValidation()
    {
      RuleFor(o => o.Instrument).NotNull().NotEmpty().WithMessage("No instrument");
      RuleFor(o => o.Size).NotNull().NotEqual(0).WithMessage("No size");
      RuleFor(o => o.Type).NotNull().NotEqual(OrderTypeEnum.None).WithMessage("No side");
      RuleFor(o => o.Orders).NotNull().WithMessage("No orders");
    }
  }

  /// <summary>
  /// Validation rules for limit orders
  /// </summary>
  public class TransactionOrderPriceValidation : AbstractValidator<ITransactionOrderModel>
  {
    private static readonly List<OrderTypeEnum?> _immediateTypes = new List<OrderTypeEnum?>
    {
      OrderTypeEnum.Market
    };

    public TransactionOrderPriceValidation()
    {
      Include(new TransactionOrderValidation());

      When(o => _immediateTypes.Contains(o.Type) == false, () => RuleFor(o => o.Price).NotNull().NotEqual(0).WithMessage("No open price"));
      When(o => Equals(o.Type, OrderSideEnum.Buy) && Equals(o.Type, OrderTypeEnum.Stop), () => RuleFor(o => o.Price).GreaterThanOrEqualTo(o => o.Instrument.PointGroups.Last().Ask).WithMessage("Buy stop is below the offer"));
      When(o => Equals(o.Type, OrderSideEnum.Sell) && Equals(o.Type, OrderTypeEnum.Stop), () => RuleFor(o => o.Price).LessThanOrEqualTo(o => o.Instrument.PointGroups.Last().Bid).WithMessage("Sell stop is above the bid"));
      When(o => Equals(o.Type, OrderSideEnum.Buy) && Equals(o.Type, OrderTypeEnum.Limit), () => RuleFor(o => o.Price).LessThanOrEqualTo(o => o.Instrument.PointGroups.Last().Ask).WithMessage("Buy limit is above the offer"));
      When(o => Equals(o.Type, OrderSideEnum.Sell) && Equals(o.Type, OrderTypeEnum.Limit), () => RuleFor(o => o.Price).GreaterThanOrEqualTo(o => o.Instrument.PointGroups.Last().Bid).WithMessage("Sell limit is below the bid"));
    }
  }
}
