namespace Shop.Core.Configuration;

internal sealed class ShopFallbackConfig
{
    public int SchemaVersion { get; set; } = 1;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UnixEpoch;
    public List<ShopFallbackStorefront> Storefronts { get; set; } = [];
    public List<ShopFallbackCategory> Categories { get; set; } = [];
    public List<ShopFallbackOffer> Offers { get; set; } = [];
}

internal sealed class ShopFallbackStorefront
{
    public string ShopType { get; set; } = string.Empty;
    public string TitleKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string SortMode { get; set; } = "priority";
}

internal sealed class ShopFallbackCategory
{
    public long Id { get; set; }
    public string ShopType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public string? DescriptionKey { get; set; }
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }
}

internal sealed class ShopFallbackOffer
{
    public long Id { get; set; }
    public string ShopType { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = "custom_equipment";
    public string ItemKey { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string? DescriptionKey { get; set; }
    public int Price { get; set; }
    public int? AmmoPrice { get; set; }
    public int AmmoAmount { get; set; } = 1;
    public int MaxPurchasesPerRound { get; set; }
    public int MaxPurchasesPerMap { get; set; }
    public int CooldownSeconds { get; set; }
    public string AccessMode { get; set; } = "everyone";
    public List<string> RequiredPrivileges { get; set; } = [];
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }
    public string SettingsJson { get; set; } = "{}";
}
