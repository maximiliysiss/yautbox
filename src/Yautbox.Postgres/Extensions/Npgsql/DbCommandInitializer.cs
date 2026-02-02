using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace System.Data.Common;

public readonly ref struct DbCommandInitializer
{
    public DbCommand Command { get; }

    public string CommandText
    {
        get => Command.CommandText;
        [param: AllowNull]
        set => Command.CommandText = value;
    }

    public int CommandTimeout
    {
        get => Command.CommandTimeout;
        set => Command.CommandTimeout = value;
    }

    public CommandType CommandType
    {
        get => Command.CommandType;
        set => Command.CommandType = value;
    }

    public DbConnection? Connection
    {
        get => Command.Connection;
        set => Command.Connection = value;
    }

    public DbParameterCollection Parameters => Command.Parameters;

    public DbTransaction? Transaction
    {
        get => Command.Transaction;
        set => Command.Transaction = value;
    }

    public UpdateRowSource UpdatedRowSource
    {
        get => Command.UpdatedRowSource;
        set => Command.UpdatedRowSource = value;
    }

    public DbCommandInitializer(DbConnection connection) : this(null, connection)
    {
    }

    public DbCommandInitializer(string? cmdText, DbConnection connection, DbTransaction? transaction = null)
    {
        Command = connection.CreateCommand();
        Command.CommandText = cmdText;
        Command.Transaction = transaction;
    }

    public static implicit operator DbCommand(DbCommandInitializer commandInitializer) => commandInitializer.Command;
}
