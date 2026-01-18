using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Yautbox.Entities;
using Yautbox.Postgres.Options;

namespace Yautbox.Postgres.Repositories;

internal class OutboxRepository : IOutboxRepository
{
    private readonly TimeProvider _timeProvider;
    private readonly IOutboxConnectionFactory _connectionFactory;
    private readonly PostgresOutboxRepositoryOptions _options;

    public OutboxRepository(
        IOutboxConnectionFactory connectionFactory,
        TimeProvider timeProvider,
        IOptions<PostgresOutboxRepositoryOptions> options)
    {
        _connectionFactory = connectionFactory;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task AddAsync<TPayload>(IEnumerable<TPayload> messages, CancellationToken cancellationToken)
    {
        var query = $"""
        INSERT INTO {_options.SchemaName}.outbox_messages(message_type, payload, created_at)
        SELECT :message_type, payload, :created_at
        FROM unnest(:messages) AS source (payload);
        """;

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "message_type", typeof(TPayload).FullName! },
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                {
                    "messages", messages.Select(m => JsonSerializer.Serialize(m, _options.JsonSerializerOptions)).ToArray(),
                    NpgsqlDbType.Array | NpgsqlDbType.Jsonb
                },
                { "created_at", _timeProvider.GetUtcNow() },
            },
        };

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async IAsyncEnumerable<OutboxMessage<TPayload>> ListAsync<TPayload>(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = $"""
        SELECT message_id, payload
        FROM {_options.SchemaName}.outbox_messages
        WHERE message_type = :message_type
        LIMIT :count
        FOR UPDATE SKIP LOCKED;
        """;

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "message_type", typeof(TPayload).FullName! },
                { "count", count },
            },
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var messageIdOrdinal = reader.GetOrdinal("message_id");
        var payloadOrdinal = reader.GetOrdinal("payload");

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new OutboxMessage<TPayload>(
                Id: (OutboxMessageId)reader.GetInt64(messageIdOrdinal),
                Payload: JsonSerializer.Deserialize<TPayload>(reader.GetString(payloadOrdinal), _options.JsonSerializerOptions)!);
        }
    }

    public async Task DeleteAsync(IEnumerable<OutboxMessageId> messageIds, CancellationToken cancellationToken)
    {
        var query = $"""
        DELETE FROM {_options.SchemaName}.outbox_messages
        WHERE message_id = any(:message_ids);
        """;

        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "message_ids", messageIds.ToLongsArray() },
            },
        };

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
