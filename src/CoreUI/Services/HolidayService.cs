using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for holiday objects.
  /// </summary>
  public partial class HolidayService : ObservableObject, IHolidayService
  {
    //constants


    //enums


    //types


    //attributes
    private IHolidayRepository m_holidayRepository;
    private Guid m_parent;
    private Holiday? m_selectedItem;

    //constructors
    public HolidayService(IHolidayRepository holidayRepository)
    {
      m_parent = Guid.Empty;
      m_holidayRepository = holidayRepository;
      m_selectedItem = null;
      Items = new ObservableCollection<Holiday>();
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
          m_holidayRepository.ParentId = value;
          OnPropertyChanged();
          _ = RefreshAsync();
        }
      }
    }

    public event EventHandler<Holiday?>? SelectedItemChanged;
    public Holiday? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public ObservableCollection<Holiday> Items { get; set; }

    //methods
    public async Task<Holiday> AddAsync(Holiday item)
    {
      var result = await m_holidayRepository.AddAsync(item);
      return result;
    }

    public async Task<bool> DeleteAsync(Holiday item)
    {
      bool result = await m_holidayRepository.DeleteAsync(item);
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_holidayRepository.GetItemsAsync();
      Items.Clear();
      foreach (var item in result) Items.Add(item);
    }

    public Task<Holiday> UpdateAsync(Holiday item)
    {
      return m_holidayRepository.UpdateAsync(item);
    }

    public Task<Holiday> CopyAsync(Holiday item) => throw new NotImplementedException();
    public Task<ImportResult> ImportAsync(ImportSettings importSettings) => throw new NotImplementedException();
    public Task<ExportResult> ExportAsync(string filename) => throw new NotImplementedException();
  }
}
