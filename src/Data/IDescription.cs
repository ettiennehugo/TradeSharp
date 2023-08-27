using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Object with both a name and description to describe it in further detail.
  /// </summary>
  public interface IDescription : IName
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Returns the unique ID used to associate descriptions in different languages with the object.
    /// </summary>
    protected internal Guid DescriptionTextId { get; set; }

    string Description { get; internal set; }

    //methods
    string DescriptionInLanguage(string threeLetterLanguageCode);
    string DescriptionInLanguage(CultureInfo cultureInfo);
  }
}
