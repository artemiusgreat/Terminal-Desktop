using Core.CollectionSpace;
using System;

namespace Core.ModelSpace
{
  public interface IChartModel : IBaseModel
  {
    /// <summary>
    /// Define vertical alignment and the center of chart
    /// </summary>
    double? ValueCenter { get; set; }

    /// <summary>
    /// Format values on the value axis
    /// </summary>
    Func<dynamic, dynamic> ShowValue { get; set; }

    /// <summary>
    /// Series
    /// </summary>
    INameCollection<string, IChartDataModel> ChartData { get; set; }
  }

  public class ChartModel : BaseModel, IChartModel
  {
    /// <summary>
    /// Define vertical alignment and the center of chart
    /// </summary>
    public virtual double? ValueCenter { get; set; }

    /// <summary>
    /// Format values on the value axis
    /// </summary>
    public virtual Func<dynamic, dynamic> ShowValue { get; set; }

    /// <summary>
    /// Series
    /// </summary>
    public virtual INameCollection<string, IChartDataModel> ChartData { get; set; } = new NameCollection<string, IChartDataModel>();
  }
}
