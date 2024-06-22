using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Service to build a tree of the brokers and their associated accounts.
  /// </summary>
  public class BrokerAccountsService : ServiceBase, IBrokerAccountsService
  {
    //constants


    //enums


    //types


    //attributes
    private ILogger<BrokerAccountsService> m_logger;
    private IPluginsService m_pluginsService;
    private IBrokerPlugin? m_brokerFilter;

    //constructors
    public BrokerAccountsService(ILogger<BrokerAccountsService> logger, IPluginsService pluginsService, IDialogService dialogService) : base(dialogService)
    {
      m_logger = logger;
      m_pluginsService = pluginsService;
      SelectedNodes = new ThreadSafeObservableCollection<ITreeNodeType<string, object>>();
      KeepAccountsOnDisconnect = false;    //per default sync service with connection status changes on brokers
      m_brokerFilter = null;
      Items = new ThreadSafeObservableCollection<object>();
      Nodes = new ThreadSafeObservableCollection<ITreeNodeType<string, object>>();

      //hook up the connection status change event
      foreach (var plugin in m_pluginsService.Items)
      {
        if (!(plugin is IBrokerPlugin broker))    //skip non-broker plugins
          continue;
        broker.AccountsUpdated += onAccountsUpdated;
      }
    }

    //finalizers
    ~BrokerAccountsService()
    {
      //service is transient so we need to unhook our event handlers to ensure it's garbage collected
      foreach (var plugin in m_pluginsService.Items)
      {
        if (!(plugin is IBrokerPlugin broker))    //skip non-broker plugins
          continue;
        broker.AccountsUpdated -= onAccountsUpdated;
      }
    }

    //interface implementations


    //properties
    public string RootNodeId => "BROKER_ACCOUNTS_ROOT";
    public Guid ParentId { get; set; }    //currently not used
    public ITreeNodeType<string, object>? SelectedNode { get; set; }
    public bool KeepAccountsOnDisconnect { get; set; }
    public IBrokerPlugin? BrokerFilter { get => m_brokerFilter; set { m_brokerFilter = value; Refresh(); } }
    public ObservableCollection<ITreeNodeType<string, object>> SelectedNodes { get; set; }
    public ObservableCollection<ITreeNodeType<string, object>> Nodes { get; internal set; }
    public ObservableCollection<object> Items { get; internal set; }

    //methods
    public void Refresh()
    {
      Nodes.Clear();
      Items.Clear();
      foreach (var plugin in m_pluginsService.Items)
      {
        if (!(plugin is IBrokerPlugin broker))    //skip non-broker plugins
          continue;
        if (m_brokerFilter != null && broker != m_brokerFilter) //skip brokers that don't match the filter
          continue;

        var brokerAccountNode = new BrokerAccountsNodeType(this, broker.Name, broker, null);
        if (m_brokerFilter == null)
        {
          Nodes.Add(brokerAccountNode);  //show broker nodes
          brokerAccountNode.Refresh();
        }
        else
        {
          //only show the accounts for the selected broker
          foreach (var account in broker.Accounts)
          {
            var accountNode = new BrokerAccountsNodeType(this, account.Name, account, null);
            Nodes.Add(accountNode);
          }
        }

        Items.Add(broker);
      }

      SelectedNode = Nodes.FirstOrDefault();
    }

    public void Refresh(IBrokerPlugin broker)
    {
      foreach (var plugin in m_pluginsService.Items)
      {
        if (!(plugin is IBrokerPlugin brokerPlugin))    //skip non-broker plugins
          continue;
        if (m_brokerFilter != null && broker != m_brokerFilter) //skip brokers that don't match the filter
          continue;
        if (brokerPlugin != broker) //skip brokers that should not be initialized
          continue;

        //find the associated broker node
        BrokerAccountsNodeType? brokerNode = null;
        foreach (var node in Nodes)
        {
          if (node.Item == broker)
          {
            brokerNode = (BrokerAccountsNodeType)node;
            break;
          }
        }

        //remove stale account nodes associated with the broker
        if (brokerNode!.Children.Count > 0) m_dialogService.PostUIUpdate(() => brokerNode!.Children.Clear());

        //refresh the broker
        m_dialogService.PostUIUpdate(() => brokerNode!.Refresh());

        break;  //skip rest of the brokers
      }
    }

    //most of the implementation methods are not used
    public bool Add(ITreeNodeType<string, object> item) { return false; }
    public bool Copy(ITreeNodeType<string, object> item) { return false; }
    public bool Delete(ITreeNodeType<string, object> item) { return false; }
    public void Refresh(string parentKey) { }
    public void Refresh(ITreeNodeType<string, object> parentNode) { }
    public bool Update(ITreeNodeType<string, object> item) { return false; }

    protected void onAccountsUpdated(object sender, AccountsUpdatedArgs e)
    {
      //only refresh when connection is established and we want to keep the accounts on disconnect
      //if (e.IsConnected == false && KeepAccountsOnDisconnect == true)
      //  return;
      IBrokerPlugin? broker = (IBrokerPlugin)sender;
      Refresh(broker);
    }
  }
}
