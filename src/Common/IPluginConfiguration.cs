using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TradeSharp.Common
{
  /// <summary>
  /// Interface to be implemented by data provider configuration classes.
  /// </summary>
  [ComVisible(true)]
  [Guid("AB62E77B-8656-4F66-B760-8C9237FBCE47")]
  public interface IPluginConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    string Name { get; set; }
    string Assembly { get; set; }
    IList<IPluginConfigurationProfile> Profiles { get; set; }

    //methods

  }
}
