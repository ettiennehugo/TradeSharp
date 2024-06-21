using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
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
      m_brokerFilter = null;
      Items = new ThreadSafeObservableCollection<object>();
      Nodes = new ThreadSafeObservableCollection<ITreeNodeType<string, object>>();
    }

    //finalizers


    //interface implementations


    //properties
    public string RootNodeId => "BROKER_ACCOUNTS_ROOT";
    public Guid ParentId { get; set; }    //currently not used
    public ITreeNodeType<string, object>? SelectedNode { get; set; }
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

    //most of the implementation methods are not used
    public bool Add(ITreeNodeType<string, object> item) { return false; }
    public bool Copy(ITreeNodeType<string, object> item) { return false; }
    public bool Delete(ITreeNodeType<string, object> item) { return false; }
    public void Refresh(string parentKey) { }
    public void Refresh(ITreeNodeType<string, object> parentNode) { }
    public bool Update(ITreeNodeType<string, object> item) { return false; }

  }
}
