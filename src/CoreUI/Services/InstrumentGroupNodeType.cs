using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{

  ////https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/ControlPages/TreeViewPage.xaml.cs

  /// <summary>
  /// Implementation of the instrument group nodes to manage instrument groups in a tree view model.
  /// </summary>
  public partial class InstrumentGroupNodeType : ObservableObject, ITreeNodeType<Guid, InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes
    private ITreeItemsService<Guid, InstrumentGroup> m_instrumentGroupService;

    //constructors
    public InstrumentGroupNodeType(ITreeItemsService<Guid, InstrumentGroup> service, InstrumentGroup item)
    {
      m_instrumentGroupService = service;
      Item = item;
      Id = Item.Id;
      ParentId = Item.ParentId;
      Children = new ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
      _ = RefreshAsync();
    }

    //finalizers


    //interface implementations
    public Task RefreshAsync()
    {
      Children.Clear();
      foreach (InstrumentGroup instrumentGroup in m_instrumentGroupService.Items)
        if (instrumentGroup.ParentId == Id) Children.Add(new InstrumentGroupNodeType(m_instrumentGroupService, instrumentGroup));
      return Task.CompletedTask;
    }

    //properties
    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private InstrumentGroup m_item;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_children;

    //methods


  }
}