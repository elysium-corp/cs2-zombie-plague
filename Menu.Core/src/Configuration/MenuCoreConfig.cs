using System.ComponentModel.DataAnnotations;

namespace Menu.Core.Configuration;

internal sealed class MenuCoreConfig
{
    [Required]
    [RegularExpression("^[a-z0-9][a-z0-9._-]{0,63}$")]
    public string ServerKey { get; set; } = "zombie-1";

    public List<string> ServerGroups { get; set; } = ["zombie"];

    [Required]
    public string DatabaseConnectionName { get; set; } = "elysium_zp_server_1";

    [Range(5, 3600)]
    public int SyncIntervalSeconds { get; set; } = 30;

    [Range(4, 64)]
    public int MaxNavigationDepth { get; set; } = 16;

    [Required]
    public string LastKnownGoodFileName { get; set; } = "menus.lkg.json";

    [Required]
    public string FallbackFileName { get; set; } = "menus.fallback.json";

    [Required]
    public string DefaultLocale { get; set; } = "ru";

    [Required]
    public string BroadcastPermission { get; set; } = "menu.audience.broadcast";

    public List<string> ReservedCommands { get; set; } =
    [
        "sw_menu_status",
        "sw_menu_reload",
        "sw_menu_validate"
    ];
}
