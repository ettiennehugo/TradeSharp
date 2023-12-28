using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TradeSharp.Data;
using TradeSharp.WinCoreUI.Common;
using CommunityToolkit.Mvvm.DependencyInjection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentView : Page
  {

    //constants


    //enums


    //types


    //attributes
    private IDatabase m_database;

    //constructors
    public InstrumentView()
    {
      m_database = Ioc.Default.GetRequiredService<IDatabase>();
      Exchanges = m_database.GetExchanges();
      Instrument = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "", InstrumentType.Stock, "", "", "", DateTime.Today, Exchange.InternationalId, new List<Guid>());
      this.InitializeComponent();
    }

    public InstrumentView(Instrument instrument)
    {
      m_database = Ioc.Default.GetRequiredService<IDatabase>();
      Exchanges = m_database.GetExchanges();
      Instrument = instrument;
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_instrumentProperty = DependencyProperty.Register("Instrument", typeof(Instrument), typeof(InstrumentView), new PropertyMetadata(null));
    public Instrument? Instrument
    {
      get => (Instrument?)GetValue(s_instrumentProperty);
      set => SetValue(s_instrumentProperty, value);
    }

    public IList<Exchange> Exchanges { get; internal set; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_type, typeof(InstrumentType));
    }

    //methods


  }
}
