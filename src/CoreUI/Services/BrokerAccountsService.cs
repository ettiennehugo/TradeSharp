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

    //constructors
    public BrokerAccountsService(ILogger<BrokerAccountsService> logger, IPluginsService pluginsService, IDialogService dialogService): base(dialogService)
    {
      m_logger = logger;
      m_pluginsService = pluginsService;
      SelectedNodes = new ThreadSafeObservableCollection<ITreeNodeType<string, object>>();
      Items = new ThreadSafeObservableCollection<object>();
      Nodes = new ThreadSafeObservableCollection<ITreeNodeType<string, object>>();
    }

    //finalizers


    //interface implementations


    //properties
    public string RootNodeId => "BROKER_ACCOUNTS_ROOT";
    public Guid ParentId { get; set; }    //currently not used
    public ITreeNodeType<string, object>? SelectedNode { get; set; }
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
        if (!(plugin is IBrokerPlugin broker))
          continue;
        var brokerNode = new BrokerAccountsNodeType(this, broker.Name, broker, null);
        Nodes.Add(brokerNode);
        Items.Add(broker);
        brokerNode.Refresh();
      }

      SelectedNode = Nodes.FirstOrDefault();
    }
    
    //most of the implementation methods are not used
    public bool Add(ITreeNodeType<string, object> item) { return false; }
    public bool Copy(ITreeNodeType<string, object> item) { return false; }
    public bool Delete(ITreeNodeType<string, object> item) { return false; }
    public void Refresh(string parentKey) {  }
    public void Refresh(ITreeNodeType<string, object> parentNode) { }
    public bool Update(ITreeNodeType<string, object> item) { return false;  }
  }
}
