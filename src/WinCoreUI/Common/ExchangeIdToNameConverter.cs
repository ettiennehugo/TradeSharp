﻿using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Common
{
  public class ExchangeIdToNameConverter : IValueConverter
  {
    //constants


    //enums


    //types


    //attributes
    private IDatabase m_database;

    //constructors
    public ExchangeIdToNameConverter()
    {
      m_database = (IDatabase)IApplication.Current.Services.GetService(typeof(IDatabase));
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      Exchange? exchange = m_database.GetExchange((Guid)value);
      if (exchange != null)
        return exchange.Name;
      else
        return "<No exchange found>";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      IList<Exchange> exchanges = m_database.GetExchanges();
      string name = (string)value;
      foreach (Exchange exchange in exchanges)
        if (name == exchange.Name) return exchange.Id;

      return Guid.Empty;
    }
  }
}
