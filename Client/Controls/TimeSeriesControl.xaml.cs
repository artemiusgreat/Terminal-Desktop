using Chart;
using Chart.ControlSpace;
using Chart.ModelSpace;
using Chart.SeriesSpace;
using Core.EnumSpace;
using Core.ModelSpace;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.ControlSpace
{
  /// <summary>
  /// Control class
  /// </summary>
  public partial class TimeSeriesControl : UserControl
  {
    private IList<IInputModel> _points = new List<IInputModel>();
    private IList<IDisposable> _disposables = new List<IDisposable>();
    private IList<ComponentComposer> _composers = new List<ComponentComposer>();
    private IDictionary<long, IInputModel> _cache = new Dictionary<long, IInputModel>();
    private IDictionary<string, IDictionary<string, ISeries>> _groups = new Dictionary<string, IDictionary<string, ISeries>>();

    /// <summary>
    /// Point groups
    /// </summary>
    public TimeSpan? Span { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public TimeSeriesControl()
    {
      InitializeComponent();

      var processors = InstanceManager<ResponseModel<IProcessorModel>>
        .Instance
        .Items;

      processors
        .Select(o => o.StateStream)
        .Merge()
        .Subscribe(message =>
        {
          if (Equals(message, StatusEnum.Active))
          {
            CreateCharts();
            CreateSubscriptions();
          }
        });

      CreateCharts();
      CreateSubscriptions();
    }

    /// <summary>
    /// Create synced group of charts
    /// </summary>
    private void CreateCharts()
    {
      _disposables.ForEach(o => o.Dispose());

      _cache.Clear();
      _points.Clear();
      _composers.Clear();
      _disposables.Clear();

      ChartAreas.Children.Clear();
      ChartAreas.RowDefinitions.Clear();

      var index = 0;

      var processors = InstanceManager<ResponseModel<IProcessorModel>>
        .Instance
        .Items;

      var chartGroups = processors
        .SelectMany(o => o.Charts)
        .GroupBy(o => o.Area)
        .ToDictionary(o => o.Key, o => o.ToList());

      _groups = chartGroups.ToDictionary(
        areaGroup => areaGroup.Key,
        areaGroup => areaGroup.Value
          .GroupBy(o => o.Name)
          .ToDictionary(
            seriesGroup => seriesGroup.Key,
            seriesGroup =>
            {
              var chartType = seriesGroup.FirstOrDefault().Shape;

              switch (chartType)
              {
                case nameof(ShapeEnum.Bar): return new BarSeries();
                case nameof(ShapeEnum.Area): return new AreaSeries();
                case nameof(ShapeEnum.Arrow): return new ArrowSeries();
                case nameof(ShapeEnum.Candle): return new CandleSeries();
              }

              return new LineSeries() as ISeries;

            }) as IDictionary<string, ISeries>);

      foreach (var area in _groups)
      {
        var chartControl = new ChartControl
        {
          Composers = _composers,
          Background = Brushes.Transparent,
          VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
          HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
        };

        var composer = new ComponentComposer
        {
          Name = area.Key,
          Groups = _groups,
          Control = chartControl,
          ValueCenter = chartGroups[area.Key].Any(o => o.Center.HasValue) ? chartGroups[area.Key].Max(o => o.Center ?? 0) : null,
          ShowIndexAction = (i) =>
          {
            var date =
              _points.ElementAtOrDefault((int)i)?.Time ??
              _points.ElementAtOrDefault(0)?.Time ??
              DateTime.Now;

            return $"{date:yyyy-MM-dd HH:mm}";
          }
        };

        _composers.Add(chartControl.Composer = composer);

        ChartAreas.RowDefinitions.Add(new RowDefinition());
        ChartAreas.Children.Add(chartControl);
        Grid.SetRow(chartControl, index++);
      }

      foreach (var composer in _composers)
      {
        Dispatcher.BeginInvoke(new Action(() =>
        {
          composer.Create();
          composer.Update();

        }), DispatcherPriority.ApplicationIdle);
      }
    }

    /// <summary>
    /// Update charts after data source change
    /// </summary>
    private void UpdateComposers()
    {
      foreach (var composer in _composers)
      {
        Dispatcher.BeginInvoke(new Action(() =>
        {
          composer.IndexDomain ??= new int[2];
          composer.IndexDomain[0] = Math.Max(_points.Count - composer.IndexCount.Value, 0);
          composer.IndexDomain[1] = Math.Max(_points.Count, composer.IndexCount.Value);
          composer.Items = _points;
          composer.Update();

        }), DispatcherPriority.ApplicationIdle);
      }
    }

    /// <summary>
    /// Initialize data and order subscriptions
    /// </summary>
    /// <returns></returns>
    private void CreateSubscriptions()
    {
      var processors = InstanceManager<ResponseModel<IProcessorModel>>.Instance.Items;
      var charts = processors.SelectMany(o => o.Charts);
      var areaGroups = charts.GroupBy(o => o.Area).ToDictionary(o => o.Key, o => o.ToList());
      var seriesGroups = charts.GroupBy(o => o.Name).ToDictionary(o => o.Key, o => o.ToList());
      var gateways = processors.SelectMany(processor => processor.Gateways);

      _disposables.Add(gateways
        .SelectMany(gateway => gateway.Account.Instruments.Values.Select(instrument => instrument.PointGroups.ItemStream))
        .Merge()
        .Subscribe(message =>
        {
          if (Equals(message.Action, ActionEnum.Create) || Equals(message.Action, ActionEnum.Update))
          {
            message.Next.Series[message.Next.Name] = message.Next;

            foreach (var seriesItem in message.Next.Series.Values)
            {
              if (seriesGroups.ContainsKey(seriesItem.Chart.Name))
              {
                UpdatePoint(seriesItem);
              }
            }

            UpdateComposers();
          }
        }));

      _disposables.Add(gateways
        .Select(gateway => gateway.Account.ActiveOrders.CollectionStream)
        .Merge()
        .Subscribe(message =>
        {
          foreach (var composer in _composers)
          {
            Dispatcher.BeginInvoke(new Action(() =>
            {
              composer.UpdateLevels(null, message
                .Next
                .Where(o => Equals(composer.Name, o.Instrument.Chart.Area))
                .Select(o => o.Price.Value)
                .ToList());

            }), DispatcherPriority.ApplicationIdle);
          }
        }));

      _disposables.Add(gateways
        .Select(gateway => gateway.Account.ActivePositions.ItemStream)
        .Merge()
        .Subscribe(message =>
        {
          var order = message.Next ?? message.Previous;

          if (Equals(message.Action, ActionEnum.Create))
          {
            var instrument = message
              .Next
              .Instrument;

            if (areaGroups.TryGetValue(instrument.Chart.Area, out List<IChartModel> chartModels))
            {
              var direction = 0.0;
              var chartModel = chartModels.FirstOrDefault(o => Equals(o.Shape, nameof(ShapeEnum.Arrow)));

              if (chartModel == null)
              {
                return;
              }

              switch (order.Type)
              {
                case TransactionTypeEnum.Buy: direction = 1.0; break;
                case TransactionTypeEnum.Sell: direction = -1.0; break;
              }

              var pointModel = new PointModel
              {
                Chart = chartModel,
                Time = _points.Last().Time,
                Bar = new PointBarModel
                {
                  Close = order.Price
                }
              };

              UpdatePoint(pointModel, direction);
              UpdateComposers();
            }
          }
        }));
    }

    /// <summary>
    /// Get existing or create new point in the chart series
    /// </summary>
    /// <param name="pointModel"></param>
    /// <param name="direction"></param>
    private void UpdatePoint(IPointModel pointModel, double direction = 0.0)
    {
      dynamic value = new ExpandoObject();

      value.Direction = direction;
      value.Low = pointModel?.Bar?.Low;
      value.High = pointModel?.Bar?.High;
      value.Open = pointModel?.Bar?.Open;
      value.Close = pointModel.Bar?.Close;
      value.Point = pointModel.Bar?.Close;

      var color = pointModel.Chart.Color;

      switch (pointModel.Chart.Shape)
      {
        case nameof(ShapeEnum.Line): value.Color = Brushes.Black.Color; break;
        case nameof(ShapeEnum.Arrow): value.Color = Brushes.Black.Color; break;
        case nameof(ShapeEnum.Bar): value.Color = value.Point > 0 ? Brushes.LimeGreen.Color : Brushes.OrangeRed.Color; break;
        case nameof(ShapeEnum.Area): value.Color = value.Point > 0 ? Brushes.LimeGreen.Color : Brushes.OrangeRed.Color; break;
        case nameof(ShapeEnum.Candle): value.Color = value.Close > value.Open ? Brushes.LimeGreen.Color : Brushes.OrangeRed.Color; break;
      }

      if (color.HasValue)
      {
        value.Color = Color.FromArgb(
          color.Value.A,
          color.Value.R,
          color.Value.G,
          color.Value.B);
      }

      // Update

      if (Span.HasValue && _cache.TryGetValue(pointModel.Time.Value.Ticks, out IInputModel updateModel))
      {
        updateModel
          .Areas[pointModel.Chart.Area]
          .Series[pointModel.Chart.Name]
          .Model = value;

        return;
      }

      // Create

      var createModel = new InputModel
      {
        Time = pointModel.Time.Value,
        Areas = new Dictionary<string, IAreaModel>()
      };

      foreach (var area in _groups)
      {
        createModel.Areas[area.Key] =
          createModel.Areas.ContainsKey(area.Key) ?
          createModel.Areas[area.Key] :
          new AreaModel
          {
            Name = area.Key,
            Series = new Dictionary<string, ISeriesModel>()
          };

        foreach (var series in area.Value)
        {
          createModel.Areas[area.Key].Series[series.Key] =
            createModel.Areas[area.Key].Series.ContainsKey(series.Key) ?
            createModel.Areas[area.Key].Series[series.Key] :
            new SeriesModel
            {
              Name = series.Key,
              Model = null
            };
        }
      }

      createModel
        .Areas[pointModel.Chart.Area]
        .Series[pointModel.Chart.Name]
        .Model = value;

      _cache[pointModel.Time.Value.Ticks] = createModel;
      _points.Add(createModel);
    }
  }
}
