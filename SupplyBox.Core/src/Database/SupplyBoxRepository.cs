using System.Text.Json;
using Npgsql;
using SupplyBox.Configuration;
using SwiftlyS2.Shared;

namespace SupplyBox.Database;

internal sealed record SupplyBoxSnapshot(long Version, SupplyBoxDocument Document, bool LegacyImported = false);

internal sealed class SupplyBoxRepository(ISwiftlyCore core)
{
    private bool _initialized;

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken token)
    {
        var info = core.Database.GetConnectionInfo("elysium_zp_server_1");
        if (!string.Equals(info.Driver, "postgresql", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SupplyBox requires the elysium_zp_server_1 PostgreSQL connection.");
        var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder
        {
            Host = info.Host, Port = info.Port > 0 ? info.Port : 5432,
            Database = info.Database, Username = info.User, Password = info.Pass,
            Timeout = 5, CommandTimeout = 5, Pooling = true
        }.ConnectionString);
        try { await connection.OpenAsync(token).ConfigureAwait(false); return connection; }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    public async Task InitializeAsync(SupplyBoxDocument seed, CancellationToken token)
    {
        if (_initialized) return;
        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
        await using (var migrationLock = new NpgsqlCommand("SELECT pg_advisory_xact_lock(917455, 1)", connection, transaction))
            await migrationLock.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        await using var resource = typeof(SupplyBoxRepository).Assembly.GetManifestResourceStream("SupplyBox.schema.sql")!;
        using var reader = new StreamReader(resource);
        await using var command = new NpgsqlCommand(await reader.ReadToEndAsync(token).ConfigureAwait(false), connection, transaction);
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        command.CommandText = "INSERT INTO supply_box.configuration(id, data) VALUES(1, @data::jsonb) ON CONFLICT (id) DO NOTHING";
        command.Parameters.AddWithValue("data", JsonSerializer.Serialize(seed, SupplyBoxDocument.Json));
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        await transaction.CommitAsync(token).ConfigureAwait(false);
        _initialized = true;
    }

    public async Task<SupplyBoxSnapshot> ReadAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("SELECT version, data::text, legacy_imported FROM supply_box.configuration WHERE id = 1", connection);
        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        if (!await reader.ReadAsync(token).ConfigureAwait(false)) throw new InvalidDataException("SupplyBox configuration is missing.");
        return new(reader.GetInt64(0), SupplyBoxDocument.Parse(reader.GetString(1)), reader.GetBoolean(2));
    }

    public async Task<bool> SaveAsync(SupplyBoxSnapshot snapshot, CancellationToken token)
    {
        snapshot.Document.Validate();
        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("""
            UPDATE supply_box.configuration SET data = @data::jsonb, version = version + 1,
                legacy_imported = @imported, updated_at = CURRENT_TIMESTAMP
            WHERE id = 1 AND version = @version
            """, connection);
        command.Parameters.AddWithValue("data", JsonSerializer.Serialize(snapshot.Document, SupplyBoxDocument.Json));
        command.Parameters.AddWithValue("version", snapshot.Version);
        command.Parameters.AddWithValue("imported", snapshot.LegacyImported);
        return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false) == 1;
    }
}
