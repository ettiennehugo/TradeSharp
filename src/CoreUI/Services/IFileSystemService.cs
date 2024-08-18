using System.IO;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// File system services.
  /// </summary>
  public interface IFileSystemService
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    /// <summary>
    /// Create a text file and return a StreamWriter to write to it.
    /// </summary>
    StreamWriter CreateText(string path);

    /// <summary>
    /// Open a file using the given file stream options and return a StreamReader to read from it.
    /// </summary>
    StreamReader OpenFile(string path, FileStreamOptions options);
  }
}
