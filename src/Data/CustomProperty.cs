using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// Custom property defined by a data object, typically kept in dictionaries.
  /// </summary>
  public partial class CustomProperty: ObservableObject
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public CustomProperty()
    {
      Name = string.Empty;
      Description = string.Empty;
      Type = typeof(object);
      Value = new object();
      Unit = string.Empty;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] string m_name;
    [ObservableProperty] string m_description;
    [ObservableProperty] Type m_type;
    [ObservableProperty] object m_value;
    [ObservableProperty] string m_unit; //unit of measure for value, blank if no specific unit of measure

    //methods



  }
}
