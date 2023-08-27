using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Named object supporting multiple languages.
  /// </summary>
  public interface IName
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Returns the unique ID used to associate descriptions in different languages with the object.
    /// </summary>
    protected internal Guid NameTextId { get; set; }

    public string Name { get; internal set; }

    //methods
    public string NameInLanguage(string threeLetterLanguageCode);
    public string NameInLanguage(CultureInfo cultureInfo);
  }
}
