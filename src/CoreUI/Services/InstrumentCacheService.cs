using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Events;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Instrument cache shared by instrument services.
  /// The cache represents a purely in-memory collection of instruments and does not reflect changes to the underlying data store.
  /// Calling services should make the changes as required.
  /// </summary>
  public class InstrumentCacheService : IInstrumentCacheService
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;
    protected IInstrumentRepository m_repository;

    //events
    public event ItemAddedEventHandler? ItemAdded;
    public event ItemUpdatedEventHandler? ItemUpdated;
    public event ItemRemovedEventHandler? ItemRemoved;
    public event RefreshEventHandler? Refreshed;

    //properties
    public ObservableCollection<Instrument> Items { get; }
    public LoadedState LoadedState { get; }

    //constructors
    public InstrumentCacheService(IDialogService dialogService, IInstrumentRepository repository)
    {
      m_dialogService = dialogService;
      m_repository = repository;
      Items = new ObservableCollection<Instrument>();
    }

    //finalizers


    //interface implementations


    //methods
    public void Refresh()
    {
      Items.Clear();
      foreach (var item in m_repository.GetItems())
        Items.Add(item);
      Refreshed?.Invoke(this, RefreshEventArgs.Empty);
    }

    public Task RefreshAsync()
    {
      return Task.Run(Refresh);
    }

    public void Add(Instrument item)
    {
      Items.Add(item);
      ItemAdded?.Invoke(this, new ItemAddedEventArgs(item));
    }

    public void Delete(Instrument item)
    {
      if (Items.Remove(item))
        ItemRemoved?.Invoke(this, new ItemRemovedEventArgs(item));
    }

    public void Update(Instrument item)
    {
      Instrument? oldItem = Items.FirstOrDefault(i => i.Equals(item));
      if (oldItem != null)
      {
        Items.Remove(oldItem);
        Items.Add(item);
        ItemUpdated?.Invoke(this, new ItemUpdatedEventArgs(item));
      }
      else
        Add(item);
    }
  }
}
