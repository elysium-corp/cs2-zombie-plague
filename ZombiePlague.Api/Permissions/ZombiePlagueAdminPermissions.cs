namespace ZombiePlague.Api.Permissions;

/// <summary>
/// Содержит ключи административных разрешений Zombie Plague.
/// </summary>
/// <remarks>
/// Разрешения используются административной системой
/// для управления доступом к действиям режима Zombie Plague.
/// </remarks>
public static class ZombiePlagueAdminPermissions
{
    /// <summary>
    /// Разрешает администратору принудительно заразить игрока.
    /// </summary>
    public const string Infect = "zombie_plague.admin.infect";

    /// <summary>
    /// Разрешает администратору принудительно вылечить игрока.
    /// </summary>
    public const string Disinfect = "zombie_plague.admin.disinfect";
    
    /// <summary>
    /// Разрешает администратору управлять специальными раундами Zombie Plague.
    /// </summary>
    public const string Round = "zombie_plague.admin.round";
}