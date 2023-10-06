using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for session objects.
  /// </summary>
  public partial class SessionService : ObservableObject, IItemsService<Session>
  {
    //constants


    //enums


    //types


    //attributes
    private ISessionRepository m_sessionRepository;
    private Guid m_parent;
    [ObservableProperty] private Session? m_selectedItem;
    [ObservableProperty] private ObservableCollection<Session> m_items;

    //constructors
    public SessionService(ISessionRepository sessionRepository)
    {
      m_parent = Guid.Empty;
      m_sessionRepository = sessionRepository;
      m_items = new ObservableCollection<Session>();
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
          _ = RefreshAsync();
        }
      }
    }

    public event EventHandler<Session>? SelectedItemChanged;

    //methods
    public async Task<Session> AddAsync(Session item)
    {
      var result = await m_sessionRepository.AddAsync(item);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<bool> DeleteAsync(Session item)
    {
      bool result = await m_sessionRepository.DeleteAsync(item.Id);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_sessionRepository.GetItemsAsync();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<Session> UpdateAsync(Session item)
    {
      return m_sessionRepository.UpdateAsync(item);
    }

    public async Task<Session> CopyAsync(Session item)
    {
      Session clone = (Session)item.Clone();
      clone.Id = Guid.NewGuid();
      var result = await m_sessionRepository.AddAsync(clone);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }
  }
}
