using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// User control to show account details with it's associated positions and orders.
  /// </summary>
  public sealed partial class AccountView : UserControl
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public AccountView()
    {
      ParentWindow = null;
      BrokerPlugin = null;    //TBD: This will lead to a crash if not set.
      Account = new EmptyAccount { Name = "No account selected" };
      CultureInfo = CultureInfo.CurrentCulture;
      this.InitializeComponent();
    }

    public AccountView(IBrokerPlugin broker, Account account)
    {
      ParentWindow = null;
      BrokerPlugin = broker;
      Account = account;

      //try to find the CultureInfo that matches the account's currency - otherwise we default to the current culture
      CultureInfo = CultureInfo.CurrentCulture;
      foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
      {
        var region = new RegionInfo(culture.Name);
        if (region.ISOCurrencySymbol == Account.Currency)
        {
          CultureInfo = culture;
          break;
        }
      }

      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Window ParentWindow { get; set; }
    public IBrokerPlugin? BrokerPlugin { get; set; } = null;
    public CultureInfo CultureInfo { get; set; } = null;
    public Account Account { get; set; } = null;

    //methods


  }
}
