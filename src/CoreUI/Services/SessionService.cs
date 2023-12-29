using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for session objects.
  /// </summary>
  public partial class SessionService : ObservableObject, ISessionService
  {
    //constants


    //enums


    //types


    //attributes
    private ISessionRepository m_sessionRepository;
    private Guid m_parent;
    private Session? m_selectedItem;

    //constructors
    public SessionService(ISessionRepository sessionRepository)
    {
      m_parent = Guid.Empty;
      m_sessionRepository = sessionRepository;
      Items = new ObservableCollection<Session>();
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId
    {
      get => m_parent;
      set
      {
        if (m_parent != value)
        {
          m_parent = value;
          m_sessionRepository.ParentId = value;
          OnPropertyChanged();
          Refresh();
        }
      }
    }

    public event EventHandler<Session?>? SelectedItemChanged;
    public Session? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public ObservableCollection<Session> Items { get; set; }

    //methods
    public bool Add(Session item)
    {     
      return m_sessionRepository.Add(item);
    }

    public bool Delete(Session item)
    {
      return m_sessionRepository.Delete(item);
    }

    public void Refresh()
    {
      var result = m_sessionRepository.GetItems();
      Items.Clear();
      foreach (var item in result) Items.Add(item);
    }

    public bool Update(Session item)
    {
      return m_sessionRepository.Update(item);
    }

    public bool Copy(Session item)
    {
      Session clone = (Session)item.Clone();
      clone.Id = Guid.NewGuid();
      return m_sessionRepository.Add(clone);
    }

    public ImportResult Import(ImportSettings importSettings) => throw new NotImplementedException();
    public ExportResult Export(string filename) => throw new NotImplementedException();
  }
}
