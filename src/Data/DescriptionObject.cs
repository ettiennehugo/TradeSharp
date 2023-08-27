using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Object that supports a translateable name and description property. The description text will be allocated by the data store and assign a guid to identify it, this guid
  /// can then be used through the IDataStore interface to translate the object name into different languages.
  /// </summary>
  public class DescriptionObject : NameObject, IDescription
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public DescriptionObject(IDataStoreService dataStore, IDataManagerService dataManager, string name, string description) : base(dataStore, dataManager, name)
    {
      Description = description;
    }

    //finalizers


    //interface implementations
    public string DescriptionInLanguage(string threeLetterLanguageCode)
    {
      return DataStore.GetText(DescriptionTextId, threeLetterLanguageCode);
    }

    public string DescriptionInLanguage(CultureInfo cultureInfo)
    {
      return DataStore.GetText(DescriptionTextId, cultureInfo.ThreeLetterISOLanguageName);
    }

    //properties
    public Guid DescriptionTextId { get; set; }
    public string Description { get; set; }

    //methods


  }
}
