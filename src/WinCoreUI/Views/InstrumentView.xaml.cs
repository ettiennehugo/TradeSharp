using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;

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
      m_database = (IDatabase)IApplication.Current.Services.GetService(typeof(IDatabase));
      Exchanges = m_database.GetExchanges();
      Instrument = new Instrument("", Instrument.DefaultAttributes, "", InstrumentType.Stock, Array.Empty<string>(), "", "", DateTime.Today, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Exchange.InternationalId, Array.Empty<Guid>(), string.Empty);
      this.InitializeComponent();
    }

    public InstrumentView(Instrument instrument)
    {
      m_database = (IDatabase)IApplication.Current.Services.GetService(typeof(IDatabase));
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
