using CustomKnife.Data.Models;

namespace CustomKnife.Data.Registrator;

internal interface IWritableKnivesRegistry
{
    void ReplaceAll(IReadOnlyCollection<IKnife> knives);
}
