using Core.CollectionSpace;
using Core.ManagerSpace;
using Core.ModelSpace;
using System;
using System.Linq;

namespace Core.IndicatorSpace
{
  /// <summary>
  /// Implementation
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ScaleIndicator : IndicatorModel<IPointModel, ScaleIndicator>
  {
    /// <summary>
    /// Number of bars to average
    /// </summary>
    public int Interval { get; set; }

    /// <summary>
    /// Bottom border of the normalized series
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Top border of the normalized series
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// Preserve last calculated value
    /// </summary>
    public IIndexCollection<IPointModel> Values { get; private set; } = new IndexCollection<IPointModel>();

    /// <summary>
    /// Preserve last calculated min value
    /// </summary>
    protected double? _min = null;

    /// <summary>
    /// Preserve last calculated max value
    /// </summary>
    protected double? _max = null;

    /// <summary>
    /// Calculate indicator value
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public override ScaleIndicator Calculate(IIndexCollection<IPointModel> collection)
    {
      var currentPoint = collection.ElementAtOrDefault(collection.Count - 1);

      if (currentPoint == null || currentPoint.Series == null)
      {
        return this;
      }

      var pointValue = currentPoint.Bar.Close ?? 0.0;

      _min = _min == null ? pointValue : Math.Min(_min.Value, pointValue);
      _max = _max == null ? pointValue : Math.Max(_max.Value, pointValue);

      var nextValue = ConversionManager.Compare(_min, _max) ? 0.0 : Min + (pointValue - _min.Value) * (Max - Min) / (_max.Value - _min.Value);

      var nextIndicatorPoint = new PointModel
      {
        Time = currentPoint.Time,
        TimeFrame = currentPoint.TimeFrame,
        Last = nextValue,
        Bar = new PointBarModel
        {
          Close = nextValue
        }
      };

      var previousIndicatorPoint = Values.ElementAtOrDefault(collection.Count - 1);

      if (previousIndicatorPoint == null)
      {
        Values.Add(nextIndicatorPoint);
      }

      Values[collection.Count - 1] = nextIndicatorPoint;

      currentPoint.Series[Name] = currentPoint.Series.TryGetValue(Name, out IPointModel seriesItem) ? seriesItem : new ScaleIndicator();
      currentPoint.Series[Name].Bar.Close = currentPoint.Series[Name].Last = CalculationManager.LinearWeightAverage(Values.Select(o => o.Bar.Close.Value), Values.Count - 1, Interval);
      currentPoint.Series[Name].Time = currentPoint.Time;
      currentPoint.Series[Name].ChartData = ChartData;

      Last = Bar.Close = currentPoint.Series[Name].Bar.Close;

      return this;
    }
  }
}
