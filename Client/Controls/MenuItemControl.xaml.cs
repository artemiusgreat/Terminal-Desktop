using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.ControlSpace
{
  public partial class MenuItemControl : UserControl
  {
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(MenuItemControl), new PropertyMetadata("HomeSolid"));
    public static readonly DependencyProperty BorderProperty = DependencyProperty.Register("Border", typeof(string), typeof(MenuItemControl), new PropertyMetadata("0,0,0,0"));
    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof(string), typeof(MenuItemControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register("Actions", typeof(IDictionary<string, Action>), typeof(MenuItemControl), new PropertyMetadata(null));

    /// <summary>
    /// Icon name
    /// </summary>
    public string Icon
    {
      get => (string)GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Border
    /// </summary>
    public string Border
    {
      get => (string)GetValue(BorderProperty);
      set => SetValue(BorderProperty, value);
    }

    /// <summary>
    /// Caption
    /// </summary>
    public string Caption
    {
      get => (string)GetValue(CaptionProperty);
      set => SetValue(CaptionProperty, value);
    }

    /// <summary>
    /// Sub menu
    /// </summary>
    public IDictionary<string, Action> Actions
    {
      get => (IDictionary<string, Action>)GetValue(ActionsProperty);
      set => SetValue(ActionsProperty, value);
    }

    /// <summary>
    /// Event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCollapse(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      e.Handled = true;

      if (Equals(SubMenuControl.Visibility, Visibility.Visible))
      {
        CollapseControl.Kind = PackIconFontAwesomeKind.ChevronCircleDownSolid;
        SubMenuControl.Visibility = Visibility.Collapsed;
        return;
      }

      CollapseControl.Kind = PackIconFontAwesomeKind.ChevronCircleUpSolid;
      SubMenuControl.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public MenuItemControl()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Click event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnListItemClick(object sender, RoutedEventArgs e)
    {
      e.Handled = true;

      if (sender is Button)
      {
        Actions[$"{ (sender as Button).Content }"]();
      }
    }

    /// <summary>
    /// Caption enter event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCaptionMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
      CaptionControl.Cursor = Cursors.Hand;
      CaptionControl.SetResourceReference(ForegroundProperty, "ForegroundLight");
    }

    /// <summary>
    /// Caption leave event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCaptionMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
      CaptionControl.Cursor = Cursors.None;
      CaptionControl.SetResourceReference(ForegroundProperty, "ForegroundMidLight");
    }
  }
}
