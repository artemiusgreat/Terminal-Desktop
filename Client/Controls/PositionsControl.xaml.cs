using Core.EnumSpace;
using Core.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;

// @TODO: Refactor copy-paste

namespace Client.ControlSpace
{
  public partial class PositionsControl : UserControl
  {
    /// <summary>
    /// Subscriptions
    /// </summary>
    protected IList<IDisposable> _disposables = new List<IDisposable>();

    /// <summary>
    /// Constructor
    /// </summary>
    public PositionsControl()
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
          if (Equals(message, StatusEnum.Connection))
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
      var subscription = accounts
        .Select(account => account.Positions.CollectionStream)
        .Merge()
        .Subscribe(message => CreateItems(accounts));

      _disposables.Add(subscription);

      CreateItems(accounts);
    }

    /// <summary>
    /// Generate table records 
    /// </summary>
    /// <param name="accounts"></param>
    protected void CreateItems(IEnumerable<IAccountModel> accounts)
    {
      var items = new List<dynamic>();
      var positions = accounts.SelectMany(account => account.Positions);

      foreach (var position in positions)
      {
        var item = new
        {
          Time = position.Time,
          Type = position.Type,
          Side = position.Side,
          Instrument = position.Instrument.Name,
          Size = string.Format("{0:0.00###}", position.Size),
          OpenPrice = string.Format("{0:0.00###}", position.OpenPrice),
          ClosePrice = string.Format("{0:0.00###}", position.ClosePrice),
          PnL = string.Format("{0:0.00###}", position.GainLoss)
        };

        items.Add(item);
      }

      Dispatcher.BeginInvoke(new Action(() => DataTable.ItemsSource = items));
    }
  }
}
