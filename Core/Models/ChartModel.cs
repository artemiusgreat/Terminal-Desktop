using Core.EnumSpace;
using FluentValidation;
using System.Drawing;

namespace Core.ModelSpace
{
  /// <summary>
  /// Generic model for time series
  /// </summary>
  public interface IChartModel : IBaseModel
  {
    /// <summary>
    /// Define vertical alignment and the center of chart
    /// </summary>
    double? Center { get; set; }

    /// <summary>
    /// Area of the chart where to display this data point
    /// </summary>
    string Area { get; set; }

    /// <summary>
    /// Type of the chart to use for the data point
    /// </summary>
    string Shape { get; set; }

    /// <summary>
    /// Primary color
    /// </summary>
    Color? Color { get; set; }
  }

  /// <summary>
  /// Expando class that allows to extend other models in runtime
  /// </summary>
  public class ChartModel : BaseModel, IChartModel
  {
    /// <summary>
    /// Define vertical alignment and the center of chart
    /// </summary>
    public virtual double? Center { get; set; }

    /// <summary>
    /// Area of the chart where to display this data point
    /// </summary>
    public virtual string Area { get; set; }

    /// <summary>
    /// Type of the chart to use for the data point
    /// </summary>
    public virtual string Shape { get; set; }

    /// <summary>
    /// Primary color
    /// </summary>
    public virtual Color? Color { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ChartModel()
    {
      Shape = nameof(ShapeEnum.Line);
    }
  }

  /// <summary>
  /// Validation rules
  /// </summary>
  public class ChartValidation : AbstractValidator<IChartModel>
  {
    public ChartValidation()
    {
      RuleFor(o => o.Area).NotNull().NotEmpty().WithMessage("No chart area");
      RuleFor(o => o.Name).NotNull().NotEmpty().WithMessage("No series name");
      RuleFor(o => o.Shape).NotNull().NotEmpty().WithMessage("No chart type");
    }
  }
}
