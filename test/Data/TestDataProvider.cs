using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data.Testing
{
  public class TestDataProvider : IDataProvider
  {

    //constants


    //enums


    //types


    //attributes
    string m_name;
    List<string> m_tickers;

    //constructors
    public TestDataProvider()
    {
      m_name = "";
      m_tickers = new List<string>();
    }

    //finalizers


    //interface implementations
    public void Connect() { }

    public void Create(string config)
    {
      m_name = config;
    }

    public void Destroy() { }

    public void Disconnect() { }

    public void Request(string ticker, DateTime start, DateTime end) { }

    //properties
    public string Name => m_name;
    public IList<string> Tickers => m_tickers;

    //methods


  }
}
