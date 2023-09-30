using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TradeSharp.CoreUI.Events
{
  /// <summary>
  /// Information about the navigation state.
  /// </summary>
  public class NavigationInfo
  {
    public bool UseNavigation { get; set; }
  }

  /// <summary>
  /// Message to be sent when a navigtion between application views should change.
  /// </summary>
  public class NavigationMessage : ValueChangedMessage<NavigationInfo>
  {
    public NavigationMessage(NavigationInfo value) : base(value) { }
  }
}
