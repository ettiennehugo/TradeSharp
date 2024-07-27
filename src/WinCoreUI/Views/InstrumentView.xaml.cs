using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentView : Page, IWindowedView
  {
    //constants
    //NOTE: Currently (07/25/24) the ActualWidth and ActualHeight is never correct after the page layout so just setting the size values statically.
    //      Can revisit this but later but for now it is not a priority.
    public const int InstrumentWidth = 920;
    public const int InstrumentHeight = 860;
    public const int StockWidth = 920;
    public const int StockHeight = 1430;

    //enums


    //types


    //attributes
    private int m_width = InstrumentWidth;
    private int m_height = InstrumentHeight;
    private IInstrumentService m_instrumentService;
    private IExchangeViewModel m_exchangeViewModel;
    private StockInstrumentView m_stockInstrumentView;

    //constructors
    public InstrumentView(InstrumentType instrumentType, ViewWindow parent)
    {
      ParentWindow = parent;
      m_exchangeViewModel = (IExchangeViewModel)IApplication.Current.Services.GetService(typeof(IExchangeViewModel));
      m_instrumentService = (IInstrumentService)IApplication.Current.Services.GetService(typeof(IInstrumentService));
      Exchanges = m_exchangeViewModel.Items;

      switch (instrumentType)
      {
        case InstrumentType.Stock:
          Instrument = new Stock("", Instrument.DefaultAttributes, "", InstrumentType.Stock, Array.Empty<string>(), "", "", DateTime.Today, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Exchange.InternationalId, Array.Empty<Guid>(), string.Empty);
          break;
        default:
          Instrument = new Instrument("", Instrument.DefaultAttributes, "", InstrumentType.None, Array.Empty<string>(), "", "", DateTime.Today, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Exchange.InternationalId, Array.Empty<Guid>(), string.Empty);
          break;
      }

      this.InitializeComponent();
      addStockView();
      setParentProperties();
    }

    public InstrumentView(Instrument instrument, ViewWindow parent)
    {
      ParentWindow = parent;
      m_exchangeViewModel = (IExchangeViewModel)IApplication.Current.Services.GetService(typeof(IExchangeViewModel));
      m_instrumentService = (IInstrumentService)IApplication.Current.Services.GetService(typeof(IInstrumentService));
      Exchanges = m_exchangeViewModel.Items;
      Instrument = instrument;
      this.InitializeComponent();
      setParentProperties();
    }

    //finalizers


    //interface implementations


    //properties
    public ViewWindow ParentWindow { get; protected set; }
    public UIElement UIElement => this;
    public static readonly DependencyProperty s_instrumentProperty = DependencyProperty.Register("Instrument", typeof(Instrument), typeof(InstrumentView), new PropertyMetadata(null));
    public Instrument? Instrument
    {
      get => (Instrument?)GetValue(s_instrumentProperty);
      set => SetValue(s_instrumentProperty, value);
    }

    public IList<Exchange> Exchanges { get; internal set; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {      
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_type, typeof(InstrumentType));
    }

    private void setParentProperties()
    {
      ParentWindow.View = this;   //need to set this only once the view screen elements are created
      ParentWindow.ResetSizeable();
      ParentWindow.HideMinimizeAndMaximizeButtons();
      ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(m_width, m_height));
      ParentWindow.CenterWindow();
    }

    /// <summary>
    /// Add additional fields for the stock instrument type.
    /// </summary>
    private void addStockView()
    {
      if (Instrument is Stock stock)
      {
        m_stockInstrumentView = new StockInstrumentView(stock);
        m_main.RowDefinitions.Add(new RowDefinition());
        m_main.Children.Add(m_stockInstrumentView);

        m_buttonBar.SetValue(Grid.RowProperty, m_main.RowDefinitions.Count - 1);  //move button bar down

        m_stockInstrumentView.SetValue(Grid.RowProperty, m_main.RowDefinitions.Count - 2);  //add stock instrument view above button bar
        m_stockInstrumentView.SetValue(Grid.ColumnProperty, 0);
        m_stockInstrumentView.SetValue(Grid.ColumnSpanProperty, 2);

        m_width = StockWidth;
        m_height = StockHeight;
      }
    }

    private void m_okButton_Click(object sender, RoutedEventArgs e)
    {
      m_instrumentService.Update(Instrument);
      ParentWindow.Close();
    }

    private void m_cancelButton_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
