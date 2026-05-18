using System.Data;
using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Rollouts;
using Microsoft.Data.SqlClient;

namespace FastFashionCatalogSync.Infrastructure.Rollouts;

public sealed class SqlProductRolloutLedger : IProductRolloutLedger
{
    private readonly string _connectionString;

    public SqlProductRolloutLedger(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Rollout control connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task AddAsync(ProductRollout rollout, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ProductRollouts
            (
                Id,
                MerchandisingVersionId,
                PreviewFingerprint,
                ScheduledFor,
                RequestedBy,
                RequestedAt,
                Status,
                ClaimedAt,
                ClaimedBy,
                ExecutedAt,
                ExecutionMessage
            )
            VALUES
            (
                @Id,
                @MerchandisingVersionId,
                @PreviewFingerprint,
                @ScheduledFor,
                @RequestedBy,
                @RequestedAt,
                @Status,
                @ClaimedAt,
                @ClaimedBy,
                @ExecutedAt,
                @ExecutionMessage
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        AddRolloutParameters(command, rollout);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductRollout>> ListAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id,
                MerchandisingVersionId,
                PreviewFingerprint,
                ScheduledFor,
                RequestedBy,
                RequestedAt,
                Status,
                ClaimedAt,
                ClaimedBy,
                ExecutedAt,
                ExecutionMessage
            FROM dbo.ProductRollouts
            ORDER BY ScheduledFor;
            """;

        return await ReadRolloutsAsync(sql, now: null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductRollout>> ClaimDueAsync(
        DateTimeOffset now,
        string claimedBy,
        TimeSpan staleClaimAge,
        CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH DueRollouts AS
            (
                SELECT Id
                FROM dbo.ProductRollouts WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE (Status = 'Scheduled' AND ScheduledFor <= @Now)
                   OR (Status = 'Publishing' AND ClaimedAt <= @StaleBefore)
            )
            UPDATE rollouts
            SET Status = 'Publishing',
                ClaimedAt = @Now,
                ClaimedBy = @ClaimedBy
            OUTPUT
                inserted.Id,
                inserted.MerchandisingVersionId,
                inserted.PreviewFingerprint,
                inserted.ScheduledFor,
                inserted.RequestedBy,
                inserted.RequestedAt,
                inserted.Status,
                inserted.ClaimedAt,
                inserted.ClaimedBy,
                inserted.ExecutedAt,
                inserted.ExecutionMessage
            FROM dbo.ProductRollouts rollouts
            INNER JOIN DueRollouts due ON rollouts.Id = due.Id;
            """;

        return await ReadRolloutsAsync(sql, now, claimedBy, now.Subtract(staleClaimAge), cancellationToken);
    }

    public async Task UpdateAsync(ProductRollout rollout, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ProductRollouts
            SET Status = @Status,
                ExecutedAt = @ExecutedAt,
                ExecutionMessage = @ExecutionMessage
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        AddGuid(command, "@Id", rollout.Id);
        AddString(command, "@Status", rollout.Status.ToString(), 32);
        AddNullableDateTimeOffset(command, "@ExecutedAt", rollout.ExecutedAt);
        AddNullableString(command, "@ExecutionMessage", rollout.ExecutionMessage, 1000);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ProductRollout>> ReadRolloutsAsync(
        string sql,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        return await ReadRolloutsAsync(sql, now, claimedBy: null, staleBefore: null, cancellationToken);
    }

    private async Task<IReadOnlyCollection<ProductRollout>> ReadRolloutsAsync(
        string sql,
        DateTimeOffset? now,
        string? claimedBy,
        DateTimeOffset? staleBefore,
        CancellationToken cancellationToken)
    {
        var rollouts = new List<ProductRollout>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        if (now is not null)
        {
            AddDateTimeOffset(command, "@Now", now.Value);
        }
        if (claimedBy is not null)
        {
            AddString(command, "@ClaimedBy", claimedBy, 128);
        }
        if (staleBefore is not null)
        {
            AddDateTimeOffset(command, "@StaleBefore", staleBefore.Value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rollouts.Add(MapRollout(reader));
        }

        return rollouts;
    }

    private static ProductRollout MapRollout(SqlDataReader reader)
    {
        var status = Enum.Parse<ProductRolloutStatus>(reader.GetString(reader.GetOrdinal("Status")));

        return ProductRollout.Restore(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetString(reader.GetOrdinal("MerchandisingVersionId")),
            reader.GetString(reader.GetOrdinal("PreviewFingerprint")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ScheduledFor")),
            reader.GetString(reader.GetOrdinal("RequestedBy")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("RequestedAt")),
            status,
            reader.IsDBNull(reader.GetOrdinal("ClaimedAt")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ClaimedAt")),
            reader.IsDBNull(reader.GetOrdinal("ClaimedBy")) ? null : reader.GetString(reader.GetOrdinal("ClaimedBy")),
            reader.IsDBNull(reader.GetOrdinal("ExecutedAt")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ExecutedAt")),
            reader.IsDBNull(reader.GetOrdinal("ExecutionMessage")) ? null : reader.GetString(reader.GetOrdinal("ExecutionMessage")));
    }

    private static void AddRolloutParameters(SqlCommand command, ProductRollout rollout)
    {
        AddGuid(command, "@Id", rollout.Id);
        AddString(command, "@MerchandisingVersionId", rollout.MerchandisingVersionId, 40);
        AddString(command, "@PreviewFingerprint", rollout.PreviewFingerprint, 64);
        AddDateTimeOffset(command, "@ScheduledFor", rollout.ScheduledFor);
        AddString(command, "@RequestedBy", rollout.RequestedBy, 256);
        AddDateTimeOffset(command, "@RequestedAt", rollout.RequestedAt);
        AddString(command, "@Status", rollout.Status.ToString(), 32);
        AddNullableDateTimeOffset(command, "@ClaimedAt", rollout.ClaimedAt);
        AddNullableString(command, "@ClaimedBy", rollout.ClaimedBy, 128);
        AddNullableDateTimeOffset(command, "@ExecutedAt", rollout.ExecutedAt);
        AddNullableString(command, "@ExecutionMessage", rollout.ExecutionMessage, 1000);
    }

    private static void AddGuid(SqlCommand command, string name, Guid value) =>
        command.Parameters.Add(name, SqlDbType.UniqueIdentifier).Value = value;

    private static void AddString(SqlCommand command, string name, string value, int length) =>
        command.Parameters.Add(name, SqlDbType.NVarChar, length).Value = value;

    private static void AddNullableString(SqlCommand command, string name, string? value, int length) =>
        command.Parameters.Add(name, SqlDbType.NVarChar, length).Value = value is null ? DBNull.Value : value;

    private static void AddDateTimeOffset(SqlCommand command, string name, DateTimeOffset value) =>
        command.Parameters.Add(name, SqlDbType.DateTimeOffset).Value = value;

    private static void AddNullableDateTimeOffset(SqlCommand command, string name, DateTimeOffset? value) =>
        command.Parameters.Add(name, SqlDbType.DateTimeOffset).Value = value is null ? DBNull.Value : value.Value;
}
