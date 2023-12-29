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
    public InstrumentGroupNodeType(IInstrumentGroupService service, InstrumentGroup item)
    {
      m_instrumentGroupService = service;
      Item = item;
      Id = Item.Id;
      ParentId = Item.ParentId;
      Children = new ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
      Refresh();
    }

    //finalizers


    //interface implementations
    public void Refresh()
    {
      Children.Clear();
      foreach (InstrumentGroup instrumentGroup in m_instrumentGroupService.Items)
        if (instrumentGroup.ParentId == Id) Children.Add(new InstrumentGroupNodeType(m_instrumentGroupService, instrumentGroup));
    }

    //properties
    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private InstrumentGroup m_item;
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> Children { get; internal set; }

    //methods


  }
}