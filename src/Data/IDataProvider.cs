using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  public interface IDataProvider
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    string Name { get; }
    IList<string> Tickers { get; }

    //methods
    void Connect();
    void Disconnect();
    void Create(string config);
    void Destroy();
    void Request(string ticker, DateTime start, DateTime end);

  }
}
