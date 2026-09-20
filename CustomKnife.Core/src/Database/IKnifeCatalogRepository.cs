using CustomKnife.Data.Models;

namespace CustomKnife.Database;

internal interface IKnifeCatalogRepository
{
    IReadOnlyCollection<IKnife> GetEnabledKnives();

    /// <summary>Читает проверенный каталог вне игрового потока с отменой при выгрузке.</summary>
    Task<IReadOnlyCollection<IKnife>> GetEnabledKnivesAsync(CancellationToken cancellationToken);
}
