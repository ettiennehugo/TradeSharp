namespace TradeSharp.Common
{
  /// <summary>
  /// Store for general configuration of a plugin. A plugin can be anything from a data base, data provider, broker or general extension.
  /// </summary>
  public class PluginConfiguration : IPluginConfiguration
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public PluginConfiguration(string name, string assembly, string type, IDictionary<string, object> configuration)
    {
      Name = name;
      Assembly = assembly;
      Type = type;
      Configuration = configuration;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public string Name { get; set; }
    public string Assembly { get; set; }
    public string Type { get; set; }
    public IDictionary<string, object> Configuration { get; set; }
  }
}
