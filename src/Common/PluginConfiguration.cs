namespace TradeSharp.Common
{
  /// <summary>
  /// Store for data provider plugins and their associated configuration profiles used to configure them.
  /// </summary>
  public class PluginConfiguration : IPluginConfiguration
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public PluginConfiguration(string name, string assembly, IList<IPluginConfigurationProfile> profiles)
    {
      Name = name;
      Assembly = assembly;
      Profiles = profiles;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public string Name { get; set; }
    public string Assembly { get; set; }
    public IList<IPluginConfigurationProfile> Profiles { get; set; }
  }
}
