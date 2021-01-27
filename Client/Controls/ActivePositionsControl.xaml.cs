using Core.EnumSpace;
using Core.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

// @TODO: Refactor copy-paste

namespace Client.ControlSpace
{
  public partial class ActivePositionsControl : UserControl
  {
    /// <summary>
    /// Subscriptions
    /// </summary>
    protected IList<IDisposable> _disposables = new List<IDisposable>();

    /// <summary>
    /// Constructor
    /// </summary>
    public ActivePositionsControl()
    {
      InitializeComponent();
      CreateSubscriptions();

      InstanceManager<ResponseModel<IProcessorModel>>
        .Instance
        .Items
        .Select(processor => processor.StateStream)
        .Merge()
        .Subscribe(message =>
        {
          if (Equals(message, StatusEnum.Active))
          {
            CreateSubscriptions();
          }
        });
    }

    /// <summary>
    /// Initialize subscriptions
    /// </summary>
    /// <returns></returns>
    protected void CreateSubscriptions()
    {
      _disposables.ForEach(o => o.Dispose());
      _disposables.Clear();

      var processors = InstanceManager<ResponseModel<IProcessorModel>>.Instance.Items;
      var accounts = processors.SelectMany(processor => processor.Gateways.Select(o => o.Account));

      var pointSubscription = accounts
        .SelectMany(account => account.Instruments.Values.Select(instrument => instrument.PointGroups.ItemStream))
        .Merge()
        .Subscribe(message => CreateItems(accounts));

      var positionSubscription = accounts
        .Select(account => account.ActivePositions.CollectionStream)
        .Merge()
        .Subscribe(message => CreateItems(accounts));

      _disposables.Add(pointSubscription);
      _disposables.Add(positionSubscription);

      CreateItems(accounts);
    }

    /// <summary>
    /// Generate table records 
    /// </summary>
    /// <param name="accounts"></param>
    protected void CreateItems(IEnumerable<IAccountModel> accounts)
    {
      var items = new List<dynamic>();
      var positions = accounts.SelectMany(account => account.ActivePositions);

      foreach (var position in positions)
      {
        var item = new
        {
          Time = position.Time,
          Side = position.Type,
          Instrument = position.Instrument.Name,
          Size = string.Format("{0:0.00###}", position.Size),
          OpenPrice = string.Format("{0:0.00###}", position.Price),
          ClosePrice = string.Format("{0:0.00###}", position.ClosePriceEstimate),
          PnL = string.Format("{0:0.00###}", position.GainLossEstimate)
        };

        items.Add(item);
      }

      Dispatcher.BeginInvoke(new Action(() => DataTable.ItemsSource = items));
    }
  }
}
