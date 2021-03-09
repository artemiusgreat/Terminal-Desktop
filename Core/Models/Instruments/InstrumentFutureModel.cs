using FluentValidation;
using System;

namespace Core.ModelSpace
{
  /// <summary>
  /// Definition
  /// </summary>
  public interface IInstrumentFutureModel : IInstrumentModel
  {
    /// <summary>
    /// Expiration date
    /// </summary>
    DateTime? ExpirationDate { get; set; }
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public class InstrumentFutureModel : InstrumentModel, IInstrumentFutureModel
  {
    /// <summary>
    /// Expiration date
    /// </summary>
    public virtual DateTime? ExpirationDate { get; set; }
  }

  /// <summary>
  /// Validation rules
  /// </summary>
  public class InstrumentFutureValidation : AbstractValidator<IInstrumentFutureModel>
  {
    public InstrumentFutureValidation()
    {
      RuleFor(o => o.ExpirationDate).NotNull().WithMessage("No expiration date");
    }
  }
}
