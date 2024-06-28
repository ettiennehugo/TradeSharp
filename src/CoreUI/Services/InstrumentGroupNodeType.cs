using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{

  ////https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/ControlPages/TreeViewPage.xaml.cs

  /// <summary>
  /// Instrument group instrument association display type, used to add the ticker and description as a tooltip to the UI.
  /// </summary>
  public partial class InstrumentGroupNodeInstrument : ObservableObject
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public InstrumentGroupNodeInstrument(string ticker, string description)
    {
      Ticker = ticker;
      Description= description;
      Highlight = false;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private string m_ticker;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private bool m_highlight;

    //methods


  }

  /// <summary>
  /// Implementation of the instrument group nodes to manage instrument groups in a tree view model.
  /// </summary>
  public partial class InstrumentGroupNodeType : ObservableObject, IInstrumentGroupNodeType
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentGroupService m_instrumentGroupService;
    private IInstrumentService m_instrumentService;

    //constructors
    public InstrumentGroupNodeType(IInstrumentGroupService instrumentGroupService, IInstrumentService instrumentService, InstrumentGroupNodeType? parent, InstrumentGroup item, bool expanded)
    {
      m_instrumentGroupService = instrumentGroupService;
      m_instrumentService = instrumentService;
      Item = item;
      Id = Item.Id;
      ParentId = Item.ParentId;
      Parent = parent;
      Instruments = new ObservableCollection<InstrumentGroupNodeInstrument>();
      Children = new ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
      Expanded = expanded;
      InstrumentsVisible = Item.Instruments.Count > 0;
      Refresh();
    }

    //finalizers


    //interface implementations
    public void Refresh()
    {
      Instruments.Clear();
      Children.Clear();
      foreach (InstrumentGroup instrumentGroup in m_instrumentGroupService.Items)
        if (instrumentGroup.ParentId == Id) Children.Add(new InstrumentGroupNodeType(m_instrumentGroupService, m_instrumentService, this, instrumentGroup, false));
      foreach (var ticker in Item.Instruments)
        Instruments.Add(new InstrumentGroupNodeInstrument(ticker, "<No description found>"));
    }

    public void RefreshInstruments()
    {
      foreach (var instrument in Instruments)
      {
        var instrumentDefinition = m_instrumentService.Items.FirstOrDefault(i => i.Equals(instrument.Ticker));
        if (instrumentDefinition != null) instrument.Description = instrumentDefinition.Description;
      }

      foreach (var child in Children)
      {
        var instrumentGroupNode = (InstrumentGroupNodeType)child;
        instrumentGroupNode.RefreshInstruments();
      }
    }

    //properties
    [ObservableProperty] private Guid m_parentId;
    [ObservableProperty] private ITreeNodeType<Guid, InstrumentGroup>? m_parent;
    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private InstrumentGroup m_item;
    [ObservableProperty] private bool m_expanded;
    [ObservableProperty] private bool m_instrumentsVisible;
    public ObservableCollection<InstrumentGroupNodeInstrument> Instruments { get; internal set; }
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> Children { get; internal set; }

    //methods


  }
}