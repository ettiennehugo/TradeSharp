using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Common
{
  /// <summary>
  /// Class encapsulating the configuration profile data specified for a specific data provider.
  /// </summary>
  public class PluginConfigurationProfile : IPluginConfigurationProfile
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public PluginConfigurationProfile(string dataProviderName, string name, string description, IDictionary<string, object> configuration)
    {
      DataProviderName = dataProviderName;
      Name = name;
      Description = description;
      Configuration = configuration;
    }

    //finalizers


    //interface implementations


    //properties
    public string DataProviderName { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public IDictionary<string, object> Configuration { get; set; }

    //methods


  }
}
