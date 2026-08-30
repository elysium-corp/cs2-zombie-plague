using Menu.Api.Results;
using Menu.Core.Runtime;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Commands;

/// <summary>
/// Выполняет открытие меню для уже зафиксированного runtime snapshot.
/// </summary>
/// <remarks>
/// Передача snapshot вместе с ключом не позволяет команде из одного Release
/// случайно открыть одноимённое меню из более нового Release.
/// </remarks>
internal interface IMenuCommandTarget
{
    /// <summary>
    /// Синхронно открывает меню, не обращаясь к БД, HTTP или файловой системе.
    /// </summary>
    /// <param name="caller">Игрок, вызвавший команду.</param>
    /// <param name="snapshot">Snapshot, в котором была найдена команда.</param>
    /// <param name="menuKey">Ключ целевого меню из той же команды.</param>
    /// <returns>Результат runtime-операции.</returns>
    MenuOperationResult OpenMenu(IPlayer caller, MenuRuntimeSnapshot snapshot, string menuKey);
}
