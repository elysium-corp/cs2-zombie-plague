using Shop.Api.Data;

namespace Shop.Core.Data;

internal enum ShopSortMode
{
    Priority,
    Price,
    Alphabetical
}

internal sealed record ShopStorefrontDefinition(
    ShopType ShopType,
    string TitleKey,
    bool Enabled,
    ShopSortMode SortMode
);

internal sealed record ShopCategoryDefinition(
    long Id,
    ShopType ShopType,
    string Key,
    string DisplayNameKey,
    string? DescriptionKey,
    bool Enabled,
    int SortOrder
);

internal sealed record ShopOfferDefinition(
    ShopOffer Contract,
    string? DescriptionKey,
    string SettingsJson
)
{
    public long Id => Contract.Id;
    public ShopType ShopType => Contract.ShopType;
    public long? CategoryId => Contract.CategoryId;
    public bool Enabled => Contract.Enabled;
}

internal sealed record ShopSnapshot(
    IReadOnlyDictionary<ShopType, ShopStorefrontDefinition> Storefronts,
    IReadOnlyList<ShopCategoryDefinition> Categories,
    IReadOnlyList<ShopOfferDefinition> Offers,
    string Source,
    DateTimeOffset LoadedAt
)
{
    public static ShopSnapshot Empty(string source = "empty")
    {
        var storefronts = Enum.GetValues<ShopType>().ToDictionary(
            type => type,
            type => new ShopStorefrontDefinition(
                type,
                type == ShopType.Human ? "Shop.Human.Title" : "Shop.Zombie.Title",
                false,
                ShopSortMode.Priority));

        return new ShopSnapshot(storefronts, [], [], source, DateTimeOffset.UtcNow);
    }
}

internal sealed class ShopSnapshotCache
{
    private ShopSnapshot _current = ShopSnapshot.Empty();

    public ShopSnapshot Current => Volatile.Read(ref _current);

    public void Replace(ShopSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref _current, snapshot);
    }
}
