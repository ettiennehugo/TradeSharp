using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.CoreUI
{
  /// <summary>
  /// Base class for all view models.
  /// </summary>
  public abstract class ViewModelBase : ObservableObject
  {
    //constants


    //enums


    //types
    private class StateSetter : IDisposable
    {
      private readonly Action _end;
      public StateSetter(Action start, Action end)
      {
        start?.Invoke();
        _end = end;
      }
      public void Dispose() => _end?.Invoke();
    }

    //attributes
    private int m_inProgressCounter = 0;
    private bool m_hasError;
    private string? m_errorMessage;

    //constructors


    //finalizers


    //interface implementations


    //properties
    
    public bool InProgress => m_inProgressCounter != 0;

    public bool HasError
    {
      get => m_hasError;
      set => SetProperty(ref m_hasError, value);
    }

    public string? ErrorMessage
    {
      get => m_errorMessage;
      set => SetProperty(ref m_errorMessage, value);
    }


    //methods
    protected void SetInProgress(bool set = true)
    {
      if (set)
      {
        Interlocked.Increment(ref m_inProgressCounter);
        OnPropertyChanged(nameof(InProgress));
      }
      else
      {
        Interlocked.Decrement(ref m_inProgressCounter);
        OnPropertyChanged(nameof(InProgress));
      }
    }

    public IDisposable StartInProgress() =>
        new StateSetter(() => SetInProgress(), () => SetInProgress(false));

  }
}
