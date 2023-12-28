using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Convert between exchange object and it's related Id.
  /// </summary>
  public class ExchangeToIdConverter : IValueConverter
  {
    //constants


    //enums


    //types


    //attributes
    private IDatabase m_database;

    //constructors
    public ExchangeToIdConverter()
    {
      m_database = Ioc.Default.GetRequiredService<IDatabase>();
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      Exchange? exchange = m_database.GetExchange((Guid)value);
      if (exchange != null)
        return exchange;
      else
        return m_database.GetExchange(Exchange.InternationalId);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      Exchange? exchange = ((Exchange?)value);
      if (exchange != null)
        return exchange.Id;
      else
        return Exchange.InternationalId;
    }
  }
}
