namespace Admin.Api.Permissions;

/// <summary>
/// Содержит стандартные ключи разрешений административной системы.
/// </summary>
/// <remarks>
/// Разрешение описывает конкретную возможность, а не роль или привилегию.
///
/// Например:
/// <list type="bullet">
/// <item><description><c>admin.kick</c> — возможность кикать игроков.</description></item>
/// <item><description><c>admin.ban</c> — возможность банить игроков.</description></item>
/// </list>
///
/// Привилегия <c>admin.owner</c>, в свою очередь, может содержать несколько таких разрешений.
/// </remarks>
public static class AdminPermissions
{
    /// <summary>
    /// Разрешает исключать игроков с сервера.
    /// </summary>
    public const string Kick = "admin.kick";
    
    /// <summary>
    /// Разрешает блокировать игроков.
    /// </summary>
    public const string Ban = "admin.ban";
}