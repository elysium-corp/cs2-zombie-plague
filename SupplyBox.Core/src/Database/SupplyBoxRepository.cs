using Npgsql;
using SupplyBox.Configuration;
using SwiftlyS2.Shared;

namespace SupplyBox.Database;

// Плагин только читает конфигурацию — подготовкой БД и изменениями управляет админка
internal sealed class SupplyBoxRepository(ISwiftlyCore core)
{
    private async Task<NpgsqlConnection> OpenAsync(CancellationToken token)
    {
        var info = core.Database.GetConnectionInfo("elysium_zp_server_1");
        if (!string.Equals(info.Driver, "postgresql", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SupplyBox требует подключение PostgreSQL elysium_zp_server_1");
        var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder
        {
            Host = info.Host, Port = info.Port > 0 ? info.Port : 5432,
            Database = info.Database, Username = info.User, Password = info.Pass,
            Timeout = 5, CommandTimeout = 5, Pooling = true
        }.ConnectionString);
        try { await connection.OpenAsync(token).ConfigureAwait(false); return connection; }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    public async Task<SupplyBoxSnapshot> ReadAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("SELECT version, data::text FROM supply_box.configuration WHERE id = 1", connection);
        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        if (!await reader.ReadAsync(token).ConfigureAwait(false))
            throw new InvalidDataException("Конфигурация SupplyBox отсутствует — подготовьте БД в админке");
        return new(reader.GetInt64(0), SupplyBoxDocument.Parse(reader.GetString(1)));
    }
}
