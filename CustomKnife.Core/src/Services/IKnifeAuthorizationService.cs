using CustomKnife.Data.Models;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Services;

/// <summary>
/// Проверяет доступ игрока к ножам по разрешениям Admin.Core.
/// </summary>
internal interface IKnifeAuthorizationService
{
    /// <summary>
    /// Определяет, может ли игрок использовать указанный нож.
    /// </summary>
    bool CanUse(IPlayer player, IKnife knife);

    /// <summary>
    /// Возвращает ключ разрешения, назначенный ножу.
    /// </summary>
    string? GetRequiredPermission(IKnife knife);
}
