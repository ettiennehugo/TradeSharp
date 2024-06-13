using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Note type for broker and accounts associated with them.
  /// </summary>
  public class BrokerAccountsNodeType : ObservableObject, ITreeNodeType<string, object>
  {
    //constants


    //enums


    //types


    //attributes
    private IBrokerAccountsService m_brokerAccountsService;

    //constructors
    public BrokerAccountsNodeType(IBrokerAccountsService brokerAccountsService, string id, object item, ITreeNodeType<string, object>? parent)
    {
      m_brokerAccountsService = brokerAccountsService;
      Id = id;
      Item = item;
      Parent = parent;
      ParentId = parent?.Id ?? m_brokerAccountsService.RootNodeId;
      Children = new ObservableCollection<ITreeNodeType<string, object>>();
    }

    //finalizers


    //interface implementations


    //properties
    public string ParentId { get; set; }
    public ITreeNodeType<string, object>? Parent { get; set; }
    public string Id { get; set; }
    public object Item { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool Expanded { get; set; }
    public ObservableCollection<ITreeNodeType<string, object>> Children { get; }

    //methods
    public void Refresh()
    {
      Children.Clear();
      Expanded = false;
      if (Item is IBrokerPlugin brokerPlugin)
        foreach (var account in brokerPlugin.Accounts)
        {
          var accountNode = new BrokerAccountsNodeType(m_brokerAccountsService, account.Name, account, this);
          Children.Add(accountNode);
        }
    }
  }
}
