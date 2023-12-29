using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.Collections.Generic;

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
    public SessionViewModel(ISessionService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      AddCommand = new RelayCommand(OnAdd, () => ParentId != Guid.Empty);
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
      CopyCommandAsync = new AsyncRelayCommand<object?>(OnCopyAsync, (object? x) => ParentId != Guid.Empty && SelectedItem != null);
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public override async void OnAdd()
    {
      Session? newSession = await m_dialogService.ShowCreateSessionAsync(m_itemsService.ParentId);
      if (newSession != null)
      {
        m_itemsService.Add(newSession);
        Items.Add(newSession);
        SelectedItem = newSession;
        OnRefresh();
      }
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateSessionAsync(SelectedItem);
        if (updatedSession != null)
        {
          m_itemsService.Update(updatedSession);
          OnRefresh();
        }
      }
    }

    public override Task OnCopyAsync(object? target)
    {
      return Task.Run(async () => {
        if (target == null) return; //should not occur if UI menu is setup correctly, just do nothing

        int copiedCount = 0;
        if (target is KeyValuePair<DayOfWeek, IList>)
        {
          KeyValuePair<DayOfWeek, IList> selectedSessions = (KeyValuePair<DayOfWeek, IList>)target;

          copiedCount = selectedSessions.Value.Count;
          Session? lastCopiedSession = null;
          foreach (Session session in selectedSessions.Value)
          {
            Session newSession = (Session)session.Clone();
            newSession.Id = Guid.NewGuid();
            newSession.DayOfWeek = selectedSessions.Key;
            m_itemsService.Add(newSession);
            Items.Add(newSession);
            lastCopiedSession = newSession;
          }

          SelectedItem = lastCopiedSession;
        }
        else if (target is KeyValuePair<Guid, IList>)
        {
          KeyValuePair<Guid, IList> selectedSessions = (KeyValuePair<Guid, IList>)target;

          copiedCount = selectedSessions.Value.Count;
          foreach (Session session in selectedSessions.Value)
          {
            Session newSession = (Session)session.Clone();
            newSession.Id = Guid.NewGuid();
            newSession.ExchangeId = selectedSessions.Key;
            m_itemsService.Add(newSession);

            //only need to add and select new session if it's added to the exact same parent
            if (ParentId == newSession.ExchangeId)
            {
              Items.Add(newSession);
              SelectedItem = newSession;
            }
          }
        }

        IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
        await dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "Success", $"Copied {copiedCount} items");
      });
    }
  }
}
