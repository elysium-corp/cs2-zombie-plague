using Microsoft.Extensions.Options;
using Shop.Core.Configuration;

namespace Shop.Core.Data;

internal sealed class FallbackShopSnapshotProvider(IOptionsMonitor<ShopFallbackConfig> options)
{
    public ShopSnapshot Load() => ShopSnapshotMapper.FromFallback(options.CurrentValue);
}
