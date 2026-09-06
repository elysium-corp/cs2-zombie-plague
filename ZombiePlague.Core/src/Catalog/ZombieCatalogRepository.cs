using Npgsql;
using SwiftlyS2.Shared;

namespace ZombiePlague.Core.Catalog;

internal sealed class ZombieCatalogRepository(ISwiftlyCore core)
{
    public async Task<ZombieCatalogState> ReadAsync(CancellationToken token)
    {
        var info = core.Database.GetConnectionInfo("elysium_zp_server_1");
        if (!string.Equals(info.Driver, "postgresql", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Каталог классов требует подключение PostgreSQL elysium_zp_server_1");
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder
        {
            Host = info.Host, Port = info.Port > 0 ? info.Port : 5432, Database = info.Database,
            Username = info.User, Password = info.Pass, Timeout = 5, CommandTimeout = 5, Pooling = true
        }.ConnectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        // Один SELECT обеспечивает согласованность классов, связей и способностей при параллельном сохранении в админке
        await using var command = new NpgsqlCommand("""
            SELECT settings.version, jsonb_build_object(
                'FormatVersion', 1, 'DefaultClass', settings.default_class, 'NemesisClass', settings.nemesis_class,
                'Classes', COALESCE((SELECT jsonb_agg(c.definition || jsonb_build_object(
                    'InternalName', c.internal_name, 'Abilities', COALESCE((
                        SELECT jsonb_agg(link.ability_key ORDER BY link.sort_order)
                        FROM zombie_plague.zombie_class_abilities link WHERE link.class_key = c.internal_name
                    ), '[]'::jsonb)) ORDER BY c.internal_name) FROM zombie_plague.zombie_classes c), '[]'::jsonb),
                'Abilities', COALESCE((SELECT jsonb_agg(a.definition || jsonb_build_object('InternalName', a.internal_name)
                    ORDER BY a.internal_name) FROM zombie_plague.zombie_abilities a), '[]'::jsonb),
                'PlayerAbilities', COALESCE((SELECT jsonb_agg(jsonb_build_object(
                    'SteamId', p.steam_id::text, 'DisplayName', p.display_name, 'Enabled', p.enabled,
                    'Abilities', COALESCE((SELECT jsonb_agg(link.ability_key ORDER BY link.sort_order)
                        FROM zombie_plague.player_abilities link WHERE link.steam_id = p.steam_id), '[]'::jsonb)
                ) ORDER BY p.steam_id) FROM zombie_plague.player_ability_assignments p), '[]'::jsonb)
            )::text
            FROM zombie_plague.class_catalog_settings settings WHERE settings.id = 1 AND settings.version > 0
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        if (!await reader.ReadAsync(token).ConfigureAwait(false))
            throw new InvalidDataException("Подготовьте каталог классов в админке");
        return new(ZombieCatalogDocument.Parse(reader.GetString(1)), reader.GetInt64(0), "database");
    }
}
