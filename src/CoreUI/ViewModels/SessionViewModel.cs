using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of sessions associated with an exchange specified as the parent id.
  /// </summary>
  public class SessionViewModel: ListViewModel<Session>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public SessionViewModel(IItemsService<Session> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      AddCommand = new RelayCommand(OnAdd, () => ParentId != Guid.Empty);
      CopyCommand = new RelayCommand(OnCopy, () => ParentId != Guid.Empty && SelectedItem != null);
    }

    //finalizers


    //interface implementations


    //properties
    public RelayCommand CopyCommand { get; internal set; }


    //methods
    public async override void OnAdd()
    {
      Session? newSession = await m_dialogService.ShowCreateSessionAsync(m_itemsService.ParentId);
      if (newSession != null)
      {
        await m_itemsService.AddAsync(newSession);
        Items.Add(newSession);
        SelectedItem = newSession;
      }
    }

    public async override void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateSessionAsync(SelectedItem);
        if (updatedSession != null)
        {
          await m_itemsService.UpdateAsync(updatedSession);
          await OnRefreshAsync();
        }
      }
    }

    public override void OnDelete()
    {
      if (SelectedItem != null)
      {
        var item = SelectedItem;
        Items.Remove(SelectedItem);
        m_itemsService.DeleteAsync(item);
        SelectedItem = Items.FirstOrDefault();
      }
    }

    public void OnCopy()
    {
      var item = SelectedItem;


      //TODO: Implement the copy session logic



    }
  }
}
