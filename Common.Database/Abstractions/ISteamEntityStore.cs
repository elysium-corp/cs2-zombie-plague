namespace Common.Database.Abstractions;

public interface ISteamEntityStore<TEntity> where TEntity : class, ISteamEntity, new()
{
    Task<TEntity?> FindAsync(ulong steamId, CancellationToken cancellationToken = default);

    Task UpsertAsync(
        ulong steamId, 
        Action<TEntity> update, 
        Action<TEntity>? initialize = null, 
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(ulong steamId, CancellationToken cancellationToken = default);
}