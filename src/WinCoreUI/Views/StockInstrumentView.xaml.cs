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

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Specific instrument fields associated with stock type instruments.
  /// This user control is created and inserted by the InstrumentView when the instrument type is Stock.
  /// </summary>
  public sealed partial class StockInstrumentView : UserControl
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    public Stock Stock { get; set; }

    //constructors
    public StockInstrumentView(Stock stock)
    {
      Stock = stock;
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //methods



  }
}
