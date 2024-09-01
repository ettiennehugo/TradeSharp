using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using TradeSharp.Common;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using System.ComponentModel;

namespace TradeSharp.WinCoreUI.Views
{
  public sealed partial class PluginsView : UserControl
  {
    //constants


    //enums


    //types


    //attributes
    private List<ICommandBarElement> m_customButtons = new List<ICommandBarElement>();
     
    //constructors
    public PluginsView()
    {
      ViewModel = (PluginsViewModel)IApplication.Current.Services.GetService(typeof(IPluginsViewModel));
      ViewModel.PropertyChanged += ViewModel_PropertyChanged;
      m_customButtons = new List<ICommandBarElement>();
      PluginsToDisplay = PluginsToDisplay.All;
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public PluginsToDisplay PluginsToDisplay { get => ViewModel.PluginsToDisplay; set { ViewModel.PluginsToDisplay = value; } }
    public IPluginsViewModel ViewModel { get; internal set; }

    //methods
    //Construct the custom command buttons associated with the plugin.
    public void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      //only update custom buttons once control is properly loaded
      if (!IsLoaded) return;

      foreach (var customButton in m_customButtons)
        m_commandBar.PrimaryCommands.Remove(customButton);
      m_customButtons.Clear();

      if (ViewModel.SelectedItem != null)
      {
        foreach (var customCommand in ViewModel.SelectedItem.Commands)
        {
          if (customCommand.Name == PluginCommand.Separator)
          {
            var separator = new AppBarSeparator();
            m_commandBar.PrimaryCommands.Add(separator);
            m_customButtons.Add(separator);
          }
          else
          {
            var button = new AppBarButton
            {
              Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 16, Glyph = customCommand.Icon },
            };

            button.SetBinding(ButtonBase.CommandProperty, new Binding { Source = customCommand.Command, Mode = BindingMode.TwoWay });
            ToolTipService.SetToolTip(button, customCommand.Tooltip);
            m_commandBar.PrimaryCommands.Add(button);
            m_customButtons.Add(button);
          }
        }
      }
    }
  }
}
