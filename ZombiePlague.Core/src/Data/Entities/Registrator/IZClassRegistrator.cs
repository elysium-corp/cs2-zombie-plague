using ZombiePlague.Core.Config.Zombie;

namespace ZombiePlague.Core.Data.Entities.Registrator;

public interface IZClassRegistrator
{
    public IEnumerable<IZClassConfig> GetAll();

    public IEnumerable<IZClassConfig> GetAllEnabled();

    public void Register();
}