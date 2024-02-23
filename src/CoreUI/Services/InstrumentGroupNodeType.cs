using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using System.Collections.ObjectModel;

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
    private IInstrumentGroupService m_instrumentGroupService;

    //constructors
    public InstrumentGroupNodeType(IInstrumentGroupService service, InstrumentGroupNodeType? parent, InstrumentGroup item, bool expanded)
    {
      m_instrumentGroupService = service;
      Item = item;
      Id = Item.Id;
      ParentId = Item.ParentId;
      Parent = parent;
      Children = new ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
      Expanded = expanded;
      InstrumentsVisible = Item.Instruments.Count > 0;
      Refresh();
    }

    //finalizers


    //interface implementations
    public void Refresh()
    {
      Children.Clear();
      foreach (InstrumentGroup instrumentGroup in m_instrumentGroupService.Items)
        if (instrumentGroup.ParentId == Id) Children.Add(new InstrumentGroupNodeType(m_instrumentGroupService, this, instrumentGroup, false));
    }

    //properties
    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private ITreeNodeType<Guid, InstrumentGroup>? m_parent;
    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private InstrumentGroup m_item;
    [ObservableProperty] private bool m_expanded;
    [ObservableProperty] private bool m_instrumentsVisible;
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> Children { get; internal set; }

    //methods


  }
}