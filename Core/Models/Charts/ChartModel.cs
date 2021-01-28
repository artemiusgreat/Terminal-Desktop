using Core.CollectionSpace;

namespace Core.ModelSpace
{
  public interface IChartModel : IBaseModel
  {
    /// <summary>
    /// Define vertical alignment and the center of chart
    /// </summary>
    double? Center { get; set; }

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
    public virtual double? Center { get; set; }

    /// <summary>
    /// Series
    /// </summary>
    public virtual INameCollection<string, IChartDataModel> ChartData { get; set; } = new NameCollection<string, IChartDataModel>();
  }
}
