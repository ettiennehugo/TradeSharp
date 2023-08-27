using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreUI
{
  /// <summary>
  /// Interface to support editing of an item object.
  /// </summary>
  public interface IEditableObject
  {

    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    void BeginEdit();
    void CancelEdit();
    void EndEdit();
  }
}
