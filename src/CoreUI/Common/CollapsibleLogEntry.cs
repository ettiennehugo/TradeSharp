using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Support collapsible log entries to display a tree type view of an ILogger.
  /// </summary>
  public class CollapsibleLogEntry: LogEntry
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public CollapsibleLogEntry(): base()
    {
      Children = new ObservableCollection<LogEntry>();
    }

    //finalizers


    //interface implementations


    //properties
    public ObservableCollection<LogEntry> Children { get; set; }

    //methods
    public override bool Matches(string filterText)
    {
      bool matches = base.Matches(filterText);

      if (!matches)
        foreach (LogEntry child in Children)
        {
          matches = child.Matches(filterText);
          if (matches) break;
        }

      return matches;
    }
  }
}
