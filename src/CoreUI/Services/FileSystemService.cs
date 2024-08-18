using System.IO;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Implementation of IFileSystemService for application use (unit tests could have their own implementations).
  /// </summary>
  public class FileSystemService : IFileSystemService
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors


    //finalizers


    //interface implementations


    //methods
    public StreamWriter CreateText(string path)
    {
      return File.CreateText(path);
    }

    public StreamReader OpenFile(string path, FileStreamOptions options)
    {
      return new StreamReader(path, options);
    }
  }
}
