using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Dispatching;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TradeSharp.Common;
using TradeSharp.WinCoreUI.Common;
using System;
using System.Linq;

namespace TradeSharp.WinCoreUI.ViewModels
{
  /// <summary>
  /// Windows specific implementatation of the InstrumentBarDataViewModel to support incremental loading on windows - not sure why these collections are tied to windows but it also means the view model needs to have a specific
  /// implementation on Windows to support it!!!
  /// </summary>
  public class InstrumentBarDataViewModel : CoreUI.ViewModels.InstrumentBarDataViewModel, IIncrementalSource<IBarData>
  {
    //constants


    //enums


    //types


    //attributes
    private int m_offsetIndex;
    private DateTime m_oldFromDateTime;
    private DateTime m_oldToDateTime;

    //constructors
    public InstrumentBarDataViewModel(IInstrumentBarDataService itemService, INavigationService navigationService, IDialogService dialogService, ILogger<InstrumentBarDataViewModel> logger) : base(itemService, navigationService, dialogService, logger)
    {
      IncrementalItems = new IncrementalObservableCollection<IBarData>(this);
      Items = IncrementalItems;
      m_offsetIndex = 0;
      HasMoreItems = true;
      IsLoading = false;
      m_oldFromDateTime = Constants.DefaultMinimumDateTime;
      m_oldToDateTime = Constants.DefaultMaximumDateTime;
    }

    //finalizers


    //interface implementations
    public override Task OnRefreshAsync()
    {
      if (!isKeyed() || IsLoading) return Task.CompletedTask;   //only one load allowed at a time

      if (Debugging.InstrumentBarDataLoadAsync) m_logger.LogInformation($"OnRefreshAsync not loading - starting reload - (Resolution: {Resolution}, ThreadId: {Thread.CurrentThread.ManagedThreadId})");
      m_offsetIndex = 0;
      HasMoreItems = true;
      IncrementalItems.RefreshAsync();

      return Task.CompletedTask;
    }

    public override Task OnDeleteAsync(object? target)
    {
      return Task.Run(() =>
      {
        int count = 0;
        if (target is IBarData)
        {
          IBarData item = (IBarData)target;
          Items.Remove(item);
          m_itemsService.Delete(item);
          SelectedItem = Items.FirstOrDefault();
          count++;
        }
        else if (target is IList<IBarData>)
        {
          IList<IBarData> items = (IList<IBarData>)target;
          foreach (IBarData item in items)
          {
            m_itemsService.Delete(item);
            count++;
          }

          OnRefresh();
          SelectedItem = Items.FirstOrDefault();
        }
        else if (target is IReadOnlyList<ItemIndexRange>)
        {
          //can not read the control content asynchronously so must run on the UI thread
          //OPTIMIZATION: Can pack the dates to be deleted into a range table and delete the whole range asynchronously.
          m_dialogService.PostUIUpdate(() =>
          {
            IReadOnlyList<ItemIndexRange> ranges = (IReadOnlyList<ItemIndexRange>)target;
            IList<IBarData> itemsToDelete = new List<IBarData>();
            foreach (ItemIndexRange range in ranges)
              for (int i = range.FirstIndex; i <= range.LastIndex; i++)
              {
                itemsToDelete.Add(Items[i]);
                count++;
              }

            m_barDataService.Delete(itemsToDelete);
            OnRefreshAsync();
            SelectedItem = Items.FirstOrDefault();
          });
        }
      });
    }

    /// <summary>
    /// Load the specified number of items from the service.
    /// </summary>
    public async Task<IList<IBarData>> LoadMoreItemsAsync(int count)
    {
      if (!isKeyed() || IsLoading) return Array.Empty<IBarData>();

      return await Task.Run(() =>
      {
        IList<IBarData> items = Array.Empty<IBarData>();

        //critical section to refresh the list
        lock (this)
        {
          if (Debugging.InstrumentBarDataLoadAsync) m_logger.LogInformation($"LoadMoreItemsAsync acquired load lock - (Resolution: {Resolution}, ThreadId: {Thread.CurrentThread.ManagedThreadId})");
          IsLoading = true;

          //force refresh for incremental loading
          if (m_fromDateTime != m_oldFromDateTime || m_toDateTime != m_oldToDateTime)
          {
            m_dialogService.PostUIUpdate(() => IncrementalItems.Clear()); //must run on the UI thread
            m_offsetIndex = 0;
            HasMoreItems = true;
            m_oldFromDateTime = m_fromDateTime;
            m_oldToDateTime = m_toDateTime;
          }

          items = m_barDataService.GetItems(m_fromDateTime, m_toDateTime, m_offsetIndex, count);

          if (Debugging.InstrumentBarDataLoadAsync)
          {
            int start = m_offsetIndex;
            int end = m_offsetIndex + items.Count;
            if (items.Count > 0)
              m_logger.LogInformation($"Loaded {items.Count} for requested count {count} range from {start} to {end} (Resolution: {Resolution}, ThreadId: {Thread.CurrentThread.ManagedThreadId}, From Date/Time: {m_fromDateTime}, To Date/Time: {m_toDateTime}, First bar date/time: {items[0].DateTime})");
            else
              m_logger.LogInformation($"Loaded {items.Count} for requested count {count} range from {start} to {end} (Resolution: {Resolution}, ThreadId: {Thread.CurrentThread.ManagedThreadId}, From Date/Time: {m_fromDateTime}, To Date/Time: {m_toDateTime}, First bar date/time: no ars loaded)");
          }

          m_offsetIndex += items.Count;
          HasMoreItems = m_offsetIndex < Count;
          IsLoading = false;
          if (Debugging.InstrumentBarDataLoadAsync) m_logger.LogInformation($"LoadMoreItemsAsync released load lock - (Resolution: {Resolution}, ThreadId: {Thread.CurrentThread.ManagedThreadId})");
        }

        return items;
      });
    }

    //properties
    /// <summary>
    /// Concrete definition of the list of incremental items, Items will be the generic definition.
    /// </summary>
    public IncrementalObservableCollection<IBarData> IncrementalItems { get; set; }

    /// <summary>
    /// Returns whether the view model has loaded all items or not.
    /// </summary>
    public bool HasMoreItems { get; internal set; }

    /// <summary>
    /// Returns whether the view model is currently loading items or not.
    /// </summary>
    public bool IsLoading { get; internal set; }

    //methods


  }
}
