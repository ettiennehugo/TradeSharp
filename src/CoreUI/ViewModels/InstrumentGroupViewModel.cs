using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of instrument groups defined and the instruments contained by them.
  /// </summary>
  public class InstrumentGroupViewModel : TreeViewModel<Guid, InstrumentGroup>, IInstrumentGroupViewModel
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentGroupService m_instrumentGroupService;
    private IInstrumentService m_instrumentService;
    private IList<ITreeNodeType<Guid, InstrumentGroup>> m_foundNodes;
    private int m_foundNodeIndex;
    private bool m_findFirstExecuted;

    //constructors
    public InstrumentGroupViewModel(IInstrumentGroupService instrumentGroupService, IInstrumentService instrumentService, INavigationService navigationService, IDialogService dialogService) : base(instrumentGroupService, navigationService, dialogService)
    {
      m_foundNodes = new List<ITreeNodeType<Guid, InstrumentGroup>>();
      m_foundNodeIndex = 0;
      m_findFirstExecuted = false;
      m_instrumentGroupService = instrumentGroupService;
      m_instrumentService = instrumentService;
      m_instrumentGroupService.RefreshEvent += onServiceRefresh;
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? target) => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Deletable));
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? target) => SelectedNode != null && SelectedNode.Item.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      Guid parentId = SelectedNode != null ? SelectedNode.Item.Id : InstrumentGroup.InstrumentGroupRoot;
      InstrumentGroup? newInstrumentGroup = await m_dialogService.ShowCreateInstrumentGroupAsync(parentId);
      if (newInstrumentGroup != null)
        m_itemsService.Add(new InstrumentGroupNodeType((IInstrumentGroupService)m_itemsService, m_instrumentService, null, newInstrumentGroup, true));   //null since parent is the root node
    }

    public override async void OnUpdate()
    {
      if (SelectedNode != null)
      {
        var updatedInstrumentGroup = await m_dialogService.ShowUpdateInstrumentGroupAsync(SelectedNode.Item);
        if (updatedInstrumentGroup != null)
        {
          SelectedNode.Item.Update(updatedInstrumentGroup);
          m_itemsService.Update(SelectedNode!);
        }
      }
      else
        await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "Please select a node to update");
    }

    protected Task OnRefreshAsync(Guid parentId)
    {
      return Task.Run(() => m_itemsService.Refresh(parentId));
    }

    public override Task OnCopyAsync(object? target)
    {
      return Task.Run(async () =>
      {
        if (SelectedNode != null)
          m_itemsService.Copy(SelectedNode);
        else
          await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", "Please select a node to copy");
      });
    }

    /// <summary>
    /// Expand all the nodes up to the root from the given node in order to display the node.
    /// </summary>
    protected virtual void expandToNode(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      ITreeNodeType<Guid, InstrumentGroup>? parentNode = node.Parent;
      Stack<ITreeNodeType<Guid, InstrumentGroup>> stack = new Stack<ITreeNodeType<Guid, InstrumentGroup>>();
      while (parentNode != null)
      {
        stack.Push(parentNode);
        parentNode = parentNode.Parent;
      }

      while (stack.Count > 0)
      {
        ITreeNodeType<Guid, InstrumentGroup> nextNode = stack.Pop();
        m_dialogService.PostUIUpdate(() => nextNode.Expanded = true);
      }

      m_dialogService.PostUIUpdate(() => SelectedNode = node);
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnClearFilter()
    {
      m_foundNodes.Clear();
      m_foundNodeIndex = 0;
      m_findFirstExecuted = false;
      NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Find the first node that matches the find text.
    /// </summary>
    public override void OnFindFirst()
    {
      m_foundNodes.Clear();
      findNodes(FindText.ToUpper(), Nodes);
      m_foundNodeIndex = 0;
      m_findFirstExecuted = true;

      if (m_foundNodes.Count > 0) 
      {
        ITreeNodeType<Guid, InstrumentGroup> firstNode = m_foundNodes[0];
        expandToNode(firstNode);
        SelectedNode = firstNode;
      }      
    }

    public override void OnFindNext()
    {
      //force search if not executed yet 
      if (m_findFirstExecuted == false)
        OnFindFirst();
      else
      {
        if (m_foundNodeIndex < m_foundNodes.Count - 1)
        {
          m_foundNodeIndex++;
          ITreeNodeType<Guid, InstrumentGroup> foundNode = m_foundNodes[m_foundNodeIndex];
          expandToNode(foundNode);
          SelectedNode = foundNode;
        }
        else
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Search complete", $"Nothing else found for search term \"{FindText}\"");
      }
    }

    public override void OnFindPrevious()
    {
      //force search if not executed yet 
      if (m_findFirstExecuted == false)
        OnFindFirst();
      else
      {
        if (m_foundNodeIndex > 0)
        {
          m_foundNodeIndex--;
          ITreeNodeType<Guid, InstrumentGroup> foundNode = m_foundNodes[m_foundNodeIndex];
          expandToNode(foundNode);
          SelectedNode = foundNode;
        }
        else
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "First Item", $"First item reached for search term \"{FindText}\"");
      }
    }

    /// <summary>
    /// Searches the tree depth first for nodes that match the find text.
    /// </summary>
    protected void findNodes(string findText, IList<ITreeNodeType<Guid, InstrumentGroup>> nodes)
    {      
      //try to match the node
      foreach (ITreeNodeType<Guid, InstrumentGroup> node in nodes)
      {
        bool nodeAdded = false;

        if (node.Item.Name.ToUpper().Contains(findText))
        {
          m_foundNodes.Add(node);
          nodeAdded = true;
        }

        if (!nodeAdded)
        {
          foreach (string alternateName in node.Item.AlternateNames)
            if (alternateName.ToUpper().Contains(findText))
            {
              m_foundNodes.Add(node);
              nodeAdded = true;
              break;
            }
        }

        if (!nodeAdded)
        {
          //NOTE: This search does NOT search the alternate tickers of the instruments only, to expand that would require
          //      passing the IInstrumentService to the nodes to retrieve the alternate tickers for the instrument. 
          //https://stackoverflow.com/questions/23316932/invoke-command-when-treeviewitem-is-expanded
          foreach (var ticker in node.Item.Instruments)
            if (ticker.Contains(findText))
            {
              m_foundNodes.Add(node);
              nodeAdded = true;
              break;
            }
        }
      }

      //recurse to the node children
      foreach (ITreeNodeType<Guid, InstrumentGroup> node in nodes) findNodes(findText, node.Children);
    }

    //properties


    //methods


  }
}
