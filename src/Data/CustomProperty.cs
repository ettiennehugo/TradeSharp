using System;
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
    public CustomProperty(object? parent, string name, string description, Type type, string unit = "")
    {
      Parent = parent;
      Name = name;
      Description = description;
      Type = type;
      Value = Activator.CreateInstance(type)!;  //only use basic types for custom properties, if this breaks you most likely used some other type
      Unit = unit;
    }

    //finalizers


    //interface implementations


    //properties
    public object? Parent { get; protected set; } //object that owns this property
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public Type Type { get; protected set; }
    [ObservableProperty] object m_value;
    public string Unit { get; protected set; } //unit of measure for value, blank if no specific unit of measure

    //methods


  }
}
