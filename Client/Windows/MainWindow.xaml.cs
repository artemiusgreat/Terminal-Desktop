using Client.ControlSpace;
using Client.StrategySpace;
using Core.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Client.WindowSpace
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    public static readonly DependencyProperty MenuSizeProp = DependencyProperty.Register("MenuSize", typeof(int), typeof(MainWindow), new PropertyMetadata(150));
    public static readonly DependencyProperty ChartActionsProp = DependencyProperty.Register("ChartActions", typeof(IDictionary<string, Action>), typeof(MainWindow), new PropertyMetadata(null));
    public static readonly DependencyProperty ControlActionsProp = DependencyProperty.Register("ControlActions", typeof(IDictionary<string, Action>), typeof(MainWindow), new PropertyMetadata(null));
    public static readonly DependencyProperty StatementActionsProp = DependencyProperty.Register("StatementActions", typeof(IDictionary<string, Action>), typeof(MainWindow), new PropertyMetadata(null));

    /// <summary>
    /// Controls menu
    /// </summary>
    public IDictionary<string, Action> ControlActions
    {
      get { return (IDictionary<string, Action>)GetValue(ControlActionsProp); }
      set { SetValue(ControlActionsProp, value); }
    }

    /// <summary>
    /// Chart menu
    /// </summary>
    public IDictionary<string, Action> ChartActions
    {
      get { return (IDictionary<string, Action>)GetValue(ChartActionsProp); }
      set { SetValue(ChartActionsProp, value); }
    }

    /// <summary>
    /// Statement menu
    /// </summary>
    public IDictionary<string, Action> StatementActions
    {
      get { return (IDictionary<string, Action>)GetValue(StatementActionsProp); }
      set { SetValue(StatementActionsProp, value); }
    }

    /// <summary>
    /// Menu width
    /// </summary>
    public int MenuSize
    {
      get { return (int)GetValue(MenuSizeProp); }
      set { SetValue(MenuSizeProp, value); }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();

      var processors = InstanceManager<ResponseModel<IProcessorModel>>.Instance.Items = new List<IProcessorModel>
      {
        new ImbalanceStrategy()
      };

      // Set controls menu

      SetCurrentValue(ControlActionsProp, new Dictionary<string, Action>
      {
        ["Connect"] = () => processors.ForEach(o => o.Connect()),
        ["Disconnect"] = () => processors.ForEach(o => o.Disconnect()),
        ["Pause"] = () => processors.ForEach(o => o.Unsubscribe()),
        ["Continue"] = () => processors.ForEach(o => o.Subscribe())
      });

      // Set charts menu

      SetCurrentValue(ChartActionsProp, new Dictionary<string, Action>
      {
        ["Time Series"] = () => OpenPopup("Time Series", new TimeSeriesControl { Span = TimeSpan.Zero })
      });

      // Set statements menu

      SetCurrentValue(StatementActionsProp, new Dictionary<string, Action>
      {
        ["Orders"] = () => OpenPopup("Orders", new OrdersControl()),
        ["Active Orders"] = () => OpenPopup("Active Orders", new ActiveOrdersControl()),
        ["Positions"] = () => OpenPopup("Positions", new PositionsControl()),
        ["Active Positions"] = () => OpenPopup("Active Positions", new ActivePositionsControl()),
        ["Statements"] = () => OpenPopup("Statements", new StatementsControl())
      });
    }

    /// <summary>
    /// Close all child windows
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnWindowClose(object sender, System.EventArgs e)
    {
      foreach (Window o in Application.Current.Windows)
      {
        o.Close();
      }
    }

    /// <summary>
    /// Event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OpenPopup(string caption, object control)
    {
      var popup = new PopupWindow
      {
        Title = caption,
        Content = control
      };

      popup.Show();
    }

    /// <summary>
    /// Open menu event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnMenuMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (Equals(MenuSize, 150))
      {
        MenuSize = 35;
        return;
      }

      MenuSize = 150;
    }
  }
}
