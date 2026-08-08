using CustomKnife.Data.Models;

namespace CustomKnife.Data.Registrator;

public interface IKnivesRegistry
{
    IReadOnlyCollection<IKnife> GetAll();

    bool TryGet(string knifeId, out IKnife knife);

    bool TryRegister(IKnife knife);

    bool Unregister(string knifeId);
}