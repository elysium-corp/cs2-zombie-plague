using SwiftlyS2.Shared.Players;

namespace Localization.Api;

/// <summary>
/// Определяет эффективный язык конкретного игрока без обращений к базе данных.
/// </summary>
public interface ILanguageResolver
{
    /// <summary>
    /// Возвращает язык игрока по приоритету: ручной выбор, язык клиента CS2, fallback сервера.
    /// </summary>
    /// <param name="player">Авторизованный игрок.</param>
    /// <returns>Нормализованный код включённого языка.</returns>
    string Resolve(IPlayer player);

    /// <summary>
    /// Ключ Shared Interface.
    /// </summary>
    public static readonly string SharedApiKey = "Localization.Api.ILanguageResolver";
}
