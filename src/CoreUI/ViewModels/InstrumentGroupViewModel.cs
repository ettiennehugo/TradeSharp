using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using System.Collections.ObjectModel;
using static System.Net.WebRequestMethods;

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
    private IList<ITreeNodeType<Guid, InstrumentGroup>> m_foundNodes;
    private int m_foundNodeIndex;

    //constructors
    public InstrumentGroupViewModel(IInstrumentGroupService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      m_foundNodes = new List<ITreeNodeType<Guid, InstrumentGroup>>();
      m_foundNodeIndex = 0;
      m_instrumentGroupService = (IInstrumentGroupService)m_itemsService;
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
        m_itemsService.Add(new InstrumentGroupNodeType((IInstrumentGroupService)m_itemsService, null, newInstrumentGroup, true));   //null since parent is the root node
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
          m_instrumentGroupService.RefreshAssociatedTickers(SelectedNode.Item, true);   //force refresh the item tickers to make sure we always have the latest data on modified instrument groups (search will not work right if we do not do this)
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
    /// Lazy load the tickers of the instrument group when the node is node is expanded. 
    /// </summary>
    public override void OnExpandNode(object? node)
    {
      base.OnExpandNode(node);
      if (node != null && node is ITreeNodeType<Guid, InstrumentGroup>)
        foreach (var childNode in ((ITreeNodeType<Guid, InstrumentGroup>)node).Children) m_instrumentGroupService.RefreshAssociatedTickers(childNode.Item);
    }

    /// <summary>
    /// Expand all the nodes up to the root from the given node in order to display the node.
    /// </summary>
    protected void expandToNode(ITreeNodeType<Guid, InstrumentGroup> node)
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
        nextNode.Expanded = true;
      }
    }

    /// <summary>
    /// Find the first node that matches the find text.
    /// </summary>
    public override void OnFindFirst()
    {
      m_foundNodes.Clear();
      findNodes(Nodes);
      m_foundNodeIndex = 0;

      if (m_foundNodes.Count > 0) 
      {
        ITreeNodeType<Guid, InstrumentGroup> firstNode = m_foundNodes[0];
        expandToNode(firstNode);
        SelectedNode = firstNode;
      }      
    }

    public override void OnFindNext()
    {
      if (m_foundNodeIndex < m_foundNodes.Count)
      {
        m_foundNodeIndex++;
        ITreeNodeType<Guid, InstrumentGroup> foundNode = m_foundNodes[m_foundNodeIndex];
        expandToNode(foundNode);
        SelectedNode = foundNode;
      }
      else
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Search complete", $"Nothing else found for search term \"{FindText}\"");
    }

    public override void OnFindPrevious()
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

    /// <summary>
    /// Searches the tree depth first for nodes that match the find text.
    /// </summary>
    protected void findNodes(IList<ITreeNodeType<Guid, InstrumentGroup>> nodes)
    {      
      //try to match the node
      string findText = FindText.ToUpper();
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
          //https://stackoverflow.com/questions/23316932/invoke-command-when-treeviewitem-is-expanded
          m_instrumentGroupService.RefreshAssociatedTickers(node.Item);
          foreach (var ticker in node.Item.SearchTickers)
            if (ticker.Contains(findText))
            {
              m_foundNodes.Add(node);
              nodeAdded = true;
              break;
            }
        }
      }

      //recurse to the node children
      foreach (ITreeNodeType<Guid, InstrumentGroup> node in nodes) findNodes(node.Children);
    }

    //properties


    //methods


  }
}
