using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class with common functions used by the SQLite database classes.
  /// </summary>
  public abstract class SqlLiteBase : ObservableObject
  {
    //constants


    //enums


    //types


    //attributes
    protected SqliteConnection m_connection;
    protected string m_databaseFile;
    protected string m_connectionString;
    protected ILogger m_logger;

    //properties


    //constructors
    public SqlLiteBase(ILogger logger, string databaseFile, bool useWAL)
    {
      m_logger = logger;
      m_databaseFile = databaseFile;

      m_connectionString = new SqliteConnectionStringBuilder()
      {
        DataSource = m_databaseFile,
        Mode = SqliteOpenMode.ReadWriteCreate,
      }.ToString();

      m_connection = new SqliteConnection(m_connectionString);
      m_connection.Open();

      //enable write-ahead-log journalling for concurrent write/read operations - https://sqlite.org/wal.html
      if (useWAL) ExecuteCommand("PRAGMA journal_mode=WAL");

      //create the data store schema
      CreateSchema();

      //create static required objects
      CreateDefaultObjects();
    }

    public SqlLiteBase(ILogger logger)
    {
      m_logger = logger;
      m_connection = new SqliteConnection();
    }

    //finalizers
    ~SqlLiteBase()
    {
      m_connection.Close();
    }

    //interface implementations
    /// <summary>
    /// Start a new transaction - NOTE: Be careful with transaction as SQLite does not support nested transactions.
    /// </summary>
    public void StartTransaction()
    {
      lock (this) ExecuteCommand("BEGIN TRANSACTION");
    }

    /// <summary>
    /// End a transaction with either a commit or rollback.
    /// </summary>
    public void EndTransaction(bool success)
    {
      lock (this)
      {
        if (success)
          ExecuteCommand("END TRANSACTION");
        else
          ExecuteCommand("ROLLBACK TRANSACTION");
      }
    }

    /// <summary>
    /// Execute non-query method on the database.
    /// timeout = a timeout of zero is interpreted as an infinite timeout.
    /// </summary>
    public int ExecuteCommand(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database non-query - ${command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteNonQuery();
    }

    public object? ExecuteScalar(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database scalar read - ${command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteScalar();
    }

    /// <summary>
    /// Execute a database reader on the database.
    /// </summary>
    public SqliteDataReader ExecuteReader(string command, int timeout = -1)
    {
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteReader();
    }

    public void BeginTransaction()
    {
      ExecuteCommand("BEGIN TRANSACTION");
    }

    public void CommitTransaction()
    {
      ExecuteCommand("COMMIT TRANSACTION");
    }

    public void RollbackTransaction()
    {
      ExecuteCommand("ROLLBACK TRANSACTION");
    }

    public void CreateTable(string name, string columns)
    {
      //https://sqlite.org/lang_createtable.html
      ExecuteCommand($"CREATE TABLE IF NOT EXISTS {name} ({columns})");
    }

    /// <summary>
    /// Utility method to create a database index on a database table using the given columns.
    /// </summary>
    public void CreateIndex(string indexName, string tableName, bool unique, string columns)
    {
      //https://sqlite.org/lang_createindex.html
      if (unique)
        ExecuteCommand($"CREATE UNIQUE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
      else
        ExecuteCommand($"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
    }

    public void DropTable(string name)
    {
      //https://sqlite.org/lang_droptable.html
      ExecuteCommand($"DROP TABLE IF EXISTS {name}");
    }

    protected void DropIndex(string name)
    {
      //https://sqlite.org/lang_dropindex.html
      ExecuteCommand($"DROP INDEX IF EXISTS {name}");
    }

    public int GetRowCount(string tableName, string where)
    {
      int count = 0;

      var command = m_connection.CreateCommand();
      command.CommandText = $"SELECT * FROM {tableName} WHERE {where}";

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read()) count++;
      }

      return count;
    }

    /// <summary>
    /// Clear the database for testing purposes and return the number of rows removed.
    /// </summary>
    public abstract int ClearDatabase();

    public string ToSqlSafeString(string value)
    {
      return value.Replace("\'", "\'\'");
    }

    public string FromSqlSafeString(string value)
    {
      return value.Replace("\'\'", "\'");
    }

    //methods
    /// <summary>
    /// Create the database schema.
    /// </summary>
    public abstract void CreateSchema();

    /// <summary>
    /// Create any default objects always expected to exist in the database.
    /// </summary>
    public virtual void CreateDefaultObjects() { }
  }
}
