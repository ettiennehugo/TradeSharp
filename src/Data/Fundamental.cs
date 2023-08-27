using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.Common;
using System.Xml.Linq;
using System.Globalization;

namespace TradeSharp.Data
{
  /// <summary>
  /// Fundamental factor definition class.
  /// </summary>
  public class Fundamental : DescriptionObject, IFundamental
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public Fundamental(IDataStoreService dataStore, IDataManagerService dataManager, string name, string description, FundamentalCategory category, FundamentalReleaseInterval releaseInterval) : base(dataStore, dataManager, name, description)
    {
      Category = category;
      ReleaseInterval = releaseInterval;
    }

    public Fundamental(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Fundamental fundamental) : this (dataStore, dataManager, fundamental.Name, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval)
    {
      Id = fundamental.Id;
      NameTextId = fundamental.NameTextId;
      DescriptionTextId = fundamental.DescriptionTextId;
      Name = DataStore.GetText(NameTextId);
      Description = DataStore.GetText(DescriptionTextId);
    }

    //finalizers


    //interface implementations


    //properties
    public FundamentalCategory Category { get; }
    public FundamentalReleaseInterval ReleaseInterval { get; }

    //methods

  }


  public class FundamentalNone : IFundamental
  {


    //constants


    //enums


    //types


    //attributes
    private static FundamentalNone m_instance;

    //constructors
    static FundamentalNone()
    {
      m_instance = new FundamentalNone();
    }

    //finalizers


    //interface implementations


    //properties
    public static FundamentalNone Instance { get => m_instance; }
    public Guid Id => Guid.Empty;
    public Guid NameTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public Guid DescriptionTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public FundamentalCategory Category => FundamentalCategory.None;
    public FundamentalReleaseInterval ReleaseInterval => FundamentalReleaseInterval.Unknown;
    public string Name { get => Common.Resources.FundamentalNoneName; set { /* nothing to do */ } }
    public string Description { get => Common.Resources.FundamentalNoneDescription; set { /* nothing to do */ } }

    //methods
    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("FundamentalNoneName", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.FundamentalNoneName;
    }

    public string DescriptionInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("FundamentalNoneDescription", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.FundamentalNoneDescription;
    }

    string IDescription.DescriptionInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("FundamentalNoneDescription", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.FundamentalNoneDescription;
    }

    string IName.NameInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("FundamentalNoneName", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.FundamentalNoneName;
    }

  }


}
