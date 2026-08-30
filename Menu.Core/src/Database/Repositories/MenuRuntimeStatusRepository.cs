using Menu.Core.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Menu.Core.Database.Repositories;

/// <summary>
/// Пишет runtime heartbeat только для актуальной session/generation пары.
/// Новый start атомарно увеличивает generation конкретного server_key.
/// </summary>
internal sealed class MenuRuntimeStatusRepository(IDbContextFactory<MenuDbContext> contextFactory)
{
    public async Task<MenuRuntimeStatusLease> StartSessionAsync(
        MenuRuntimeStatusRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.ServerKey);

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
            var now = DateTimeOffset.UtcNow;

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO menu.server_status AS current
                    (server_key, runtime_session_id, generation, menu_core_version,
                     swiftly_version, menu_api_version, schema_version, capabilities,
                     validation_status, heartbeat_at, updated_at)
                VALUES
                    ({registration.ServerKey}, {registration.RuntimeSessionId}, 1,
                     {registration.MenuCoreVersion}, {registration.SwiftlyVersion},
                     {registration.MenuApiVersion}, {registration.SchemaVersion},
                     CAST({registration.CapabilitiesJson} AS jsonb),
                     {MenuDatabaseValues.ValidationNotLoaded}, {now}, {now})
                ON CONFLICT (server_key) DO UPDATE SET
                    runtime_session_id = EXCLUDED.runtime_session_id,
                    generation = current.generation + 1,
                    menu_core_version = EXCLUDED.menu_core_version,
                    swiftly_version = EXCLUDED.swiftly_version,
                    menu_api_version = EXCLUDED.menu_api_version,
                    schema_version = EXCLUDED.schema_version,
                    capabilities = EXCLUDED.capabilities,
                    active_release_id = NULL,
                    active_checksum = NULL,
                    loaded_source = NULL,
                    last_db_sync_at = NULL,
                    lkg_release_id = NULL,
                    fallback_release_id = NULL,
                    validation_status = EXCLUDED.validation_status,
                    last_error = NULL,
                    heartbeat_at = EXCLUDED.heartbeat_at,
                    updated_at = EXCLUDED.updated_at
                """,
                cancellationToken).ConfigureAwait(false);

            var generation = await context.ServerStatuses
                .Where(status => status.ServerKey == registration.ServerKey
                    && status.RuntimeSessionId == registration.RuntimeSessionId)
                .Select(status => status.Generation)
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new MenuRuntimeStatusLease(
                registration.ServerKey,
                registration.RuntimeSessionId,
                generation);
        }).ConfigureAwait(false);
    }

    public async Task<bool> HeartbeatAsync(
        MenuRuntimeStatusLease lease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        var now = DateTimeOffset.UtcNow;
        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var affected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE menu.server_status
            SET heartbeat_at = {now}, updated_at = {now}
            WHERE server_key = {lease.ServerKey}
              AND runtime_session_id = {lease.RuntimeSessionId}
              AND generation = {lease.Generation}
            """,
            cancellationToken).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task<bool> UpdateAsync(
        MenuRuntimeStatusLease lease,
        MenuRuntimeStatusUpdate update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(update);
        var now = DateTimeOffset.UtcNow;
        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var affected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE menu.server_status
            SET active_release_id = {update.ActiveReleaseId},
                active_checksum = {update.ActiveChecksum},
                loaded_source = {update.LoadedSource},
                last_db_sync_at = {update.LastDbSyncAt},
                lkg_release_id = {update.LastKnownGoodReleaseId},
                fallback_release_id = {update.FallbackReleaseId},
                validation_status = {update.ValidationStatus},
                last_error = {update.LastError},
                heartbeat_at = {now},
                updated_at = {now}
            WHERE server_key = {lease.ServerKey}
              AND runtime_session_id = {lease.RuntimeSessionId}
              AND generation = {lease.Generation}
            """,
            cancellationToken).ConfigureAwait(false);
        return affected == 1;
    }
}
