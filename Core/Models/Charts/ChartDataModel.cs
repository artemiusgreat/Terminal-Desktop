using System.Drawing;

namespace Core.ModelSpace
{
  public interface IChartDataModel : IBaseModel
  {
    /// <summary>
    /// Area
    /// </summary>
    string Area { get; set; }

    /// <summary>
    /// Type of the chart to use for the data point
    /// </summary>
    string Shape { get; set; }

    /// <summary>
    /// Color
    /// </summary>
    Color? Color { get; set; }
  }

  public class ChartDataModel : BaseModel, IChartDataModel
  {
    /// <summary>
    /// Area
    /// </summary>
    public virtual string Area { get; set; }

    /// <summary>
    /// Type of the chart to use for the data point
    /// </summary>
    public virtual string Shape { get; set; }

    /// <summary>
    /// Color
    /// </summary>
    public virtual Color? Color { get; set; }
  }
}
