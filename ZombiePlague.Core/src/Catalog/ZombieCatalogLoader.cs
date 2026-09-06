namespace ZombiePlague.Core.Catalog;

internal sealed record ZombieCatalogState(ZombieCatalogDocument Document, long Version, string Source,
    Exception? DatabaseError = null, Exception? FallbackError = null)
{
    private readonly Dictionary<string, ZombieAbilityDefinition> _abilities = Document.Abilities
        .ToDictionary(item => item.InternalName, StringComparer.Ordinal);
    private readonly Dictionary<string, PlayerAbilityAssignment> _players = Document.PlayerAbilities
        .ToDictionary(item => item.SteamId, StringComparer.Ordinal);

    public ZombieAbilityDefinition[] ResolveAbilities(IEnumerable<string> classAbilities, ulong steamId, AbilitySide side)
    {
        var personal = _players.TryGetValue(steamId.ToString(System.Globalization.CultureInfo.InvariantCulture), out var assignment)
            && assignment.Enabled ? assignment.Abilities : Enumerable.Empty<string>();
        // Объединение по ID происходит до создания объектов, поэтому у совпадения один hook и один cooldown
        return classAbilities.Concat(personal).Distinct(StringComparer.Ordinal)
            .Select(key => _abilities.GetValueOrDefault(key)).OfType<ZombieAbilityDefinition>()
            .Where(item => item.Enabled && (item.Side == "both" || item.Side == (side == AbilitySide.Human ? "human" : "zombie")))
            .ToArray();
    }
}

internal enum AbilitySide { Human, Zombie }

internal static class ZombieCatalogLoader
{
    public static async Task<ZombieCatalogState> LoadAsync(
        Func<CancellationToken, Task<ZombieCatalogState>> database,
        Func<CancellationToken, Task<ZombieCatalogDocument>> fallback,
        ZombieCatalogState? previous, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var state = await database(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            state.Document.Validate();
            return state;
        }
        catch (Exception databaseError) when (!token.IsCancellationRequested)
        {
            try
            {
                var document = await fallback(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                document.Validate();
                return new(document, 0, "fallback", databaseError);
            }
            catch (Exception fallbackError) when (!token.IsCancellationRequested && previous is not null)
            {
                return previous with { Source = "memory", DatabaseError = databaseError, FallbackError = fallbackError };
            }
        }
    }
}
