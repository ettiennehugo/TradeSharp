using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Data;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for holiday objects.
  /// </summary>
  public partial class HolidayService : ServiceBase, IHolidayService
  {
    //constants


    //enums


    //types


    //attributes
    private IHolidayRepository m_holidayRepository;
    private Guid m_parent;
    private Holiday? m_selectedItem;

    //constructors
    public HolidayService(IHolidayRepository holidayRepository, IDialogService dialogService): base(dialogService)
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
          Refresh();
        }
      }
    }

    public event EventHandler<Holiday?>? SelectedItemChanged;
    public Holiday? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public IList<Holiday> Items { get; set; }

    //methods
    public bool Add(Holiday item)
    {
      return m_holidayRepository.Add(item);
    }

    public bool Delete(Holiday item)
    {
      return m_holidayRepository.Delete(item);
    }

    public void Refresh()
    {
      var result = m_holidayRepository.GetItems();
      Items.Clear();
      foreach (var item in result) Items.Add(item);
    }

    public bool Update(Holiday item)
    {
      return m_holidayRepository.Update(item);
    }

    public bool Copy(Holiday item) => throw new NotImplementedException();
  }
}
