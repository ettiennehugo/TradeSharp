namespace TradeSharp.Data
{


  //TBD: Is there a way to abstract this even more? That the tradeable instrument types are defined in the data model.
  //     Abstracting this would make it very powerful in terms of the data model but will add additional processing
  //     overhead to deal with generic data/types.
  //     Something like a generic definition of the types and the attributes defining the type might be a good generic option.
  //     the actual modelled types would then become property objects that act as containers to store the generic data. Another
  //     option might be to allow the specification of the data model and then generate and compile the C# code to support
  //     those instruments, for now this would be overkill.


  /// <summary>
  /// Type of a tradeable instrument.
  /// </summary>
  public enum InstrumentType
  {
    None = 0,   //only used for initialization
    Stock,
    Forex,
    Crypto,
    Future,
    Option,
  }

  /// <summary>
  /// Base interface for tradeable instruments, e.g. Stock, Forex, Future etc. Concrete interfaces inheriting from IInstrument will implement functionality specific to the
  /// tradeable instruments.
  /// </summary>
  public interface IInstrument : IName, IDescription
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid Id { get; }
    IExchange PrimaryExchange { get; }
    IList<IExchange> SecondaryExchanges { get; }
    IList<IInstrumentFundamental> Fundamentals { get; }
    string Ticker { get; }
    InstrumentType Type { get; }
    DateTime InceptionDate { get; }
    IList<IInstrumentGroup> InstrumentGroups { get; }

    //methods

  }
}