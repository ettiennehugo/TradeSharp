using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Object that supports a translateable name property. The name text will be allocated by the data store and assign a guid to identify it, this guid
  /// can then be used through the IDataStore interface to translate the object name into different languages.
  /// </summary>
  public class NameObject : DataManagerObject, IName
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public NameObject(IDataStoreService dataStore, IDataManagerService dataManager, string name) : base(dataStore, dataManager)
    {
      Name = name.Trim();
    }

    //finalizers


    //interface implementations
    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return DataStore.GetText(NameTextId, threeLetterLanguageCode);
    }

    public string NameInLanguage(CultureInfo cultureInfo)
    {
      return NameInLanguage(cultureInfo.ThreeLetterISOLanguageName);
    }

    //properties
    public Guid NameTextId { get; set; }
    public string Name { get; set; }

    //methods


  }

}
