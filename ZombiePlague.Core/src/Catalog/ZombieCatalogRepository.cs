using System.Text.Json;
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
        ZombieCatalogState? state;
        try { state = await ReadStoredAsync(connection, token).ConfigureAwait(false); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.InvalidSchemaName)
        { state = null; }
        if (state is null || state.Document.FormatVersion == 1)
            state = await InitializeAsync(connection, token).ConfigureAwait(false);
        state.Document.Validate();
        return state;
    }

    internal async Task<ZombieCatalogState> InitializeAsync(NpgsqlConnection connection, CancellationToken token)
    {
        await using var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
        // Тот же замок используется импортом и сохранением сайта; повторно читаем каталог после его получения
        await ExecuteAsync(connection, "SELECT pg_advisory_xact_lock(917456, 1)", token).ConfigureAwait(false);
        await ExecuteAsync(connection, ReadSql("schema.sql"), token).ConfigureAwait(false);
        var existing = await ReadStoredAsync(connection, token, locked: true).ConfigureAwait(false);
        if (existing is not null && existing.Document.FormatVersion != 1)
        {
            existing.Document.Validate();
            await transaction.CommitAsync(token).ConfigureAwait(false);
            return existing;
        }
        if (existing is null)
        {
            await using var occupied = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM zombie_plague.zombie_classes) OR EXISTS(SELECT 1 FROM zombie_plague.zombie_abilities)", connection);
            if (await occupied.ExecuteScalarAsync(token).ConfigureAwait(false) is true)
                throw new InvalidDataException("В БД есть классы или способности без опубликованных настроек — восстановите class_catalog_settings из резервной копии");
        }
        var document = existing?.Document ?? await CatalogFallback.ReadAsync(core, token).ConfigureAwait(false);
        CatalogUpgrade.Apply(document, document.FormatVersion == 1 ? LegacyZombieCatalog.ReadHumans(core.Configuration.BasePath) : new());
        document.Validate();
        await WriteCatalogAsync(connection, document, token).ConfigureAwait(false);
        await SeedLocalizationAsync(connection, document, token).ConfigureAwait(false);
        var version = (existing?.Version ?? 0) + 1;
        await using var settings = new NpgsqlCommand("""
            INSERT INTO zombie_plague.class_catalog_settings(id, version, format_version, default_class, nemesis_class, default_human_class, survivor_class)
            VALUES (1, @version, 2, @normal, @nemesis, @human, @survivor)
            ON CONFLICT (id) DO UPDATE SET version = EXCLUDED.version, format_version = 2,
                default_class = EXCLUDED.default_class, nemesis_class = EXCLUDED.nemesis_class,
                default_human_class = EXCLUDED.default_human_class, survivor_class = EXCLUDED.survivor_class, updated_at = CURRENT_TIMESTAMP
            """, connection);
        settings.Parameters.AddWithValue("version", version);
        settings.Parameters.AddWithValue("normal", document.DefaultClass);
        settings.Parameters.AddWithValue("nemesis", document.NemesisClass);
        settings.Parameters.AddWithValue("human", document.DefaultHumanClass);
        settings.Parameters.AddWithValue("survivor", document.SurvivorClass);
        await settings.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        await transaction.CommitAsync(token).ConfigureAwait(false);
        return new(document, version, "database");
    }

    private static async Task<ZombieCatalogState?> ReadStoredAsync(NpgsqlConnection connection, CancellationToken token, bool locked = false)
    {
        // Один SELECT возвращает согласованный снимок классов, способностей и всех назначений
        await using var command = new NpgsqlCommand(ReadSql("read.sql") + (locked ? " FOR UPDATE OF settings" : ""), connection);
        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        if (!await reader.ReadAsync(token).ConfigureAwait(false)) return null;
        var json = reader.GetString(2);
        ZombieCatalogDocument.Require(System.Text.Encoding.UTF8.GetByteCount(json) <= ZombieCatalogDocument.MaximumBytes, "Каталог превышает 2 МБ");
        var document = JsonSerializer.Deserialize<ZombieCatalogDocument>(json, ZombieCatalogDocument.JsonOptions)
            ?? throw new InvalidDataException("Каталог классов пуст");
        return new(document, reader.GetInt64(0), "database");
    }

    private static async Task WriteCatalogAsync(NpgsqlConnection connection, ZombieCatalogDocument document, CancellationToken token)
    {
        // Записывается полный проверенный снимок под замком; определения не удаляются при миграции
        foreach (var ability in document.Abilities)
            await UpsertAsync(connection, "zombie_abilities", ability.InternalName, JsonSerializer.Serialize(ability), token).ConfigureAwait(false);
        foreach (var item in document.Classes)
        {
            await UpsertAsync(connection, "zombie_classes", item.InternalName, JsonSerializer.Serialize(item), token).ConfigureAwait(false);
            await using var clear = new NpgsqlCommand("DELETE FROM zombie_plague.zombie_class_abilities WHERE class_key = @key", connection);
            clear.Parameters.AddWithValue("key", item.InternalName);
            await clear.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            for (var i = 0; i < item.Abilities.Count; i++)
            {
                await using var link = new NpgsqlCommand("INSERT INTO zombie_plague.zombie_class_abilities(class_key, ability_key, sort_order) VALUES (@class, @ability, @position)", connection);
                link.Parameters.AddWithValue("class", item.InternalName);
                link.Parameters.AddWithValue("ability", item.Abilities[i]);
                link.Parameters.AddWithValue("position", i);
                await link.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }
        foreach (var item in document.PlayerAbilities)
        {
            await using var player = new NpgsqlCommand("""
                INSERT INTO zombie_plague.player_ability_assignments(steam_id, display_name, enabled) VALUES(@steam, @name, @enabled)
                ON CONFLICT (steam_id) DO UPDATE SET display_name = EXCLUDED.display_name, enabled = EXCLUDED.enabled
                """, connection);
            player.Parameters.AddWithValue("steam", long.Parse(item.SteamId, System.Globalization.CultureInfo.InvariantCulture));
            player.Parameters.AddWithValue("name", item.DisplayName);
            player.Parameters.AddWithValue("enabled", item.Enabled);
            await player.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            await using var clear = new NpgsqlCommand("DELETE FROM zombie_plague.player_abilities WHERE steam_id = @steam", connection);
            clear.Parameters.AddWithValue("steam", long.Parse(item.SteamId, System.Globalization.CultureInfo.InvariantCulture));
            await clear.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            for (var i = 0; i < item.Abilities.Count; i++)
            {
                await using var link = new NpgsqlCommand("INSERT INTO zombie_plague.player_abilities(steam_id, ability_key, sort_order) VALUES(@steam, @ability, @position)", connection);
                link.Parameters.AddWithValue("steam", long.Parse(item.SteamId, System.Globalization.CultureInfo.InvariantCulture));
                link.Parameters.AddWithValue("ability", item.Abilities[i]);
                link.Parameters.AddWithValue("position", i);
                await link.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }
    }

    private static async Task UpsertAsync(NpgsqlConnection connection, string table, string key, string json, CancellationToken token)
    {
        // Имя таблицы задаётся только константами выше, значения передаются параметрами
        await using var command = new NpgsqlCommand($"INSERT INTO zombie_plague.{table}(internal_name, definition) VALUES (@key, @data::jsonb - 'InternalName' - 'Abilities') ON CONFLICT (internal_name) DO UPDATE SET definition = EXCLUDED.definition", connection);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("data", json);
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
    }

    private static async Task SeedLocalizationAsync(NpgsqlConnection connection, ZombieCatalogDocument document, CancellationToken token)
    {
        await using var available = new NpgsqlCommand("SELECT to_regclass('localization.entries') IS NOT NULL AND to_regclass('localization.translations') IS NOT NULL AND to_regclass('localization.languages') IS NOT NULL", connection);
        if (await available.ExecuteScalarAsync(token).ConfigureAwait(false) is not true) return;
        var entries = document.Classes.SelectMany(item => new[] { (item.DisplayNameKey, item.DisplayName), (item.DescriptionKey, item.Description) })
            .Concat(document.Abilities.SelectMany(item => new[] { (item.DisplayNameKey, item.DisplayName), (item.DescriptionKey, item.Description) }));
        foreach (var (key, value) in entries.Where(item => !string.IsNullOrWhiteSpace(item.Item2)))
        {
            await using var command = new NpgsqlCommand(ReadSql("localization-seed.sql"), connection);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("text", value);
            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken token)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
    }

    internal static string ReadSql(string name)
    {
        using var stream = typeof(ZombieCatalogRepository).Assembly.GetManifestResourceStream($"ZombiePlague.Core.src.Catalog.{name}")
            ?? throw new InvalidOperationException($"Не найден SQL каталога: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
