using System.Data;
using FastFashionCatalogSync.Application.Abstractions;
using FastFashionCatalogSync.Domain.Releases;
using Microsoft.Data.SqlClient;

namespace FastFashionCatalogSync.Infrastructure.Releases;

public sealed class SqlCatalogReleaseLedger : ICatalogReleaseLedger
{
    private readonly string _connectionString;

    public SqlCatalogReleaseLedger(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Release control connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task AddAsync(CatalogRelease release, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.CatalogReleases
            (
                Id,
                MerchandisingVersionId,
                PreviewFingerprint,
                ScheduledFor,
                RequestedBy,
                RequestedAt,
                Status,
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
                @ExecutedAt,
                @ExecutionMessage
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        AddReleaseParameters(command, release);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogRelease>> ListAsync(CancellationToken cancellationToken)
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
                ExecutedAt,
                ExecutionMessage
            FROM dbo.CatalogReleases
            ORDER BY ScheduledFor;
            """;

        return await ReadReleasesAsync(sql, now: null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogRelease>> ListDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
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
                ExecutedAt,
                ExecutionMessage
            FROM dbo.CatalogReleases
            WHERE Status = 'Scheduled'
              AND ScheduledFor <= @Now
            ORDER BY ScheduledFor;
            """;

        return await ReadReleasesAsync(sql, now, cancellationToken);
    }

    public async Task UpdateAsync(CatalogRelease release, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CatalogReleases
            SET Status = @Status,
                ExecutedAt = @ExecutedAt,
                ExecutionMessage = @ExecutionMessage
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        AddGuid(command, "@Id", release.Id);
        AddString(command, "@Status", release.Status.ToString(), 32);
        AddNullableDateTimeOffset(command, "@ExecutedAt", release.ExecutedAt);
        AddNullableString(command, "@ExecutionMessage", release.ExecutionMessage, 1000);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<CatalogRelease>> ReadReleasesAsync(
        string sql,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        var releases = new List<CatalogRelease>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        if (now is not null)
        {
            AddDateTimeOffset(command, "@Now", now.Value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            releases.Add(MapRelease(reader));
        }

        return releases;
    }

    private static CatalogRelease MapRelease(SqlDataReader reader)
    {
        var status = Enum.Parse<CatalogReleaseStatus>(reader.GetString(reader.GetOrdinal("Status")));

        return CatalogRelease.Restore(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetString(reader.GetOrdinal("MerchandisingVersionId")),
            reader.GetString(reader.GetOrdinal("PreviewFingerprint")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ScheduledFor")),
            reader.GetString(reader.GetOrdinal("RequestedBy")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("RequestedAt")),
            status,
            reader.IsDBNull(reader.GetOrdinal("ExecutedAt")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ExecutedAt")),
            reader.IsDBNull(reader.GetOrdinal("ExecutionMessage")) ? null : reader.GetString(reader.GetOrdinal("ExecutionMessage")));
    }

    private static void AddReleaseParameters(SqlCommand command, CatalogRelease release)
    {
        AddGuid(command, "@Id", release.Id);
        AddString(command, "@MerchandisingVersionId", release.MerchandisingVersionId, 40);
        AddString(command, "@PreviewFingerprint", release.PreviewFingerprint, 64);
        AddDateTimeOffset(command, "@ScheduledFor", release.ScheduledFor);
        AddString(command, "@RequestedBy", release.RequestedBy, 256);
        AddDateTimeOffset(command, "@RequestedAt", release.RequestedAt);
        AddString(command, "@Status", release.Status.ToString(), 32);
        AddNullableDateTimeOffset(command, "@ExecutedAt", release.ExecutedAt);
        AddNullableString(command, "@ExecutionMessage", release.ExecutionMessage, 1000);
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
