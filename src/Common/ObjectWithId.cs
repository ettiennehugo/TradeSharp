using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Common
{
  /// <summary>
  /// Object with a unique identifier, typically used for objects that represent real world entities like countries, exchanges, sessions etc. 
  /// Used to uniquely identify objects and to facilitate the creation relationships between objects on the persistance layer in order to
  /// reconstruct the data between runs of the plaform.
  /// Abstract objects used to facilitate trading platform functionality can inherit directly from Object.
  /// </summary>
  public class ObjectWithId
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public ObjectWithId() : base()
    {
      Id = Guid.NewGuid();  //we generate an Id on instantiation but the persistance layer can typically set this when objects are loaded again.
    }

    //finalizers


    //interface implementations


    //properties
    public Guid Id { get; set; }

    //methods


  }
}
