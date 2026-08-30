using Menu.Core.Database.Entities;
using Menu.Core.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Menu.Core.Database.Repositories;

/// <summary>
/// Синхронизирует Provider registry с БД. Offline/heartbeat защищены парой
/// session_id + generation, поэтому событие старого plugin handle не затрёт новую загрузку.
/// </summary>
internal sealed class ProviderStateRepository(IDbContextFactory<MenuDbContext> contextFactory)
{
    public async Task<IReadOnlyList<MenuProviderValidationEntry>> LoadValidationEntriesAsync(
        string serverKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        var instances = await context.ProviderInstances
            .AsNoTracking()
            .Include(instance => instance.Provider)
            .Include(instance => instance.Exports)
            .Where(instance => instance.ServerKey == serverKey)
            .OrderBy(instance => instance.Provider.ProviderKey)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return instances
            .Select(instance => new MenuProviderValidationEntry(
                instance.Provider.ProviderKey,
                instance.Provider.DisplayName,
                instance.ServerKey,
                instance.PluginVersion,
                instance.MenuApiVersion,
                instance.Status,
                instance.CapabilitiesJson,
                instance.MetadataJson,
                instance.SessionId,
                instance.Generation,
                instance.RegisteredAt,
                instance.LastSeenAt,
                instance.OfflineAt,
                instance.UpdatedAt,
                instance.LastError,
                instance.Exports
                    .OrderBy(export => export.ExportType, StringComparer.Ordinal)
                    .ThenBy(export => export.ExportKey, StringComparer.Ordinal)
                    .Select(export => new MenuProviderExportValidationEntry(
                        export.ExportType,
                        export.ExportKey,
                        export.DisplayName,
                        export.SchemaJson,
                        export.MetadataJson,
                        export.IsDeclared,
                        export.DeclaredGeneration,
                        export.FirstSeenAt,
                        export.LastSeenAt,
                        export.UpdatedAt))
                    .ToArray()))
            .ToArray();
    }

    public async Task ReconcileAsync(
        string serverKey,
        MenuProviderPersistenceSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);
        ArgumentNullException.ThrowIfNull(snapshot);

        await using var strategyContext = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        var strategy = strategyContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow;

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO menu.providers
                    (provider_key, display_name, metadata, created_at, updated_at)
                VALUES
                    ({snapshot.ProviderKey}, {snapshot.DisplayName},
                     CAST({snapshot.MetadataJson} AS jsonb), {now}, {now})
                ON CONFLICT (provider_key) DO UPDATE SET
                    display_name = EXCLUDED.display_name,
                    metadata = EXCLUDED.metadata,
                    updated_at = EXCLUDED.updated_at
                """,
                cancellationToken).ConfigureAwait(false);

            var providerId = await context.Providers
                .Where(provider => provider.ProviderKey == snapshot.ProviderKey)
                .Select(provider => provider.Id)
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO menu.provider_instances AS current
                    (provider_id, server_key, plugin_version, menu_api_version, status,
                     capabilities, metadata, session_id, generation, registered_at,
                     last_seen_at, offline_at, updated_at, last_error)
                VALUES
                    ({providerId}, {serverKey}, {snapshot.PluginVersion}, {snapshot.MenuApiVersion},
                     {snapshot.Status},
                     CAST({snapshot.CapabilitiesJson} AS jsonb),
                     CAST({snapshot.MetadataJson} AS jsonb),
                     {snapshot.SessionId}, {snapshot.Generation}, {now}, {now}, NULL, {now}, {snapshot.LastError})
                ON CONFLICT (provider_id, server_key) DO UPDATE SET
                    plugin_version = EXCLUDED.plugin_version,
                    menu_api_version = EXCLUDED.menu_api_version,
                    status = EXCLUDED.status,
                    capabilities = EXCLUDED.capabilities,
                    metadata = EXCLUDED.metadata,
                    session_id = EXCLUDED.session_id,
                    generation = EXCLUDED.generation,
                    registered_at = CASE
                        WHEN current.session_id = EXCLUDED.session_id
                            THEN current.registered_at
                        ELSE EXCLUDED.registered_at
                    END,
                    last_seen_at = EXCLUDED.last_seen_at,
                    offline_at = NULL,
                    updated_at = EXCLUDED.updated_at,
                    last_error = EXCLUDED.last_error
                """,
                cancellationToken).ConfigureAwait(false);

            var instance = await context.ProviderInstances
                .Include(providerInstance => providerInstance.Exports)
                .SingleAsync(
                    providerInstance => providerInstance.ProviderId == providerId
                        && providerInstance.ServerKey == serverKey
                        && providerInstance.SessionId == snapshot.SessionId
                        && providerInstance.Generation == snapshot.Generation,
                    cancellationToken)
                .ConfigureAwait(false);

            foreach (var existing in instance.Exports)
            {
                existing.IsDeclared = false;
                existing.UpdatedAt = now;
            }

            var existingByKey = instance.Exports.ToDictionary(
                export => (export.ExportType, export.ExportKey));
            foreach (var export in snapshot.Exports)
            {
                if (!existingByKey.TryGetValue((export.ExportType, export.ExportKey), out var entity))
                {
                    entity = new MenuProviderExportEntity
                    {
                        ProviderInstanceId = instance.Id,
                        ExportType = export.ExportType,
                        ExportKey = export.ExportKey,
                        FirstSeenAt = now
                    };
                    context.ProviderExports.Add(entity);
                }

                entity.DisplayName = export.DisplayName;
                entity.SchemaJson = export.SchemaJson;
                entity.MetadataJson = export.MetadataJson;
                entity.IsDeclared = true;
                entity.DeclaredGeneration = snapshot.Generation;
                entity.LastSeenAt = now;
                entity.UpdatedAt = now;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<bool> HeartbeatAsync(
        string serverKey,
        string providerKey,
        Guid sessionId,
        long generation,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var affected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE menu.provider_instances AS instance
            SET last_seen_at = {now}, updated_at = {now}
            FROM menu.providers AS provider
            WHERE instance.provider_id = provider.id
              AND instance.server_key = {serverKey}
              AND provider.provider_key = {providerKey}
              AND instance.session_id = {sessionId}
              AND instance.generation = {generation}
              AND instance.status = {MenuDatabaseValues.ProviderStatusOnline}
            """,
            cancellationToken).ConfigureAwait(false);

        return affected == 1;
    }

    public async Task<bool> MarkOfflineAsync(
        string serverKey,
        string providerKey,
        Guid sessionId,
        long generation,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await using var strategyContext = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        var strategy = strategyContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            var affected = await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE menu.provider_instances AS instance
                SET status = {MenuDatabaseValues.ProviderStatusOffline},
                    offline_at = {now}, last_seen_at = {now}, updated_at = {now}
                FROM menu.providers AS provider
                WHERE instance.provider_id = provider.id
                  AND instance.server_key = {serverKey}
                  AND provider.provider_key = {providerKey}
                  AND instance.session_id = {sessionId}
                  AND instance.generation = {generation}
                """,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return affected == 1;
        }).ConfigureAwait(false);
    }
}
