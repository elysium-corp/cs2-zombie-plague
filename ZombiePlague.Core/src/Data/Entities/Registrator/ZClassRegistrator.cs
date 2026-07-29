using Microsoft.Extensions.Options;
using ZombiePlague.Core.Config.Zombie;

namespace ZombiePlague.Core.Data.Entities.Registrator;

internal class ZClassRegistrator(IOptions<ZClassConfig> config) : IZClassRegistrator
{
    private readonly List<IZClassConfig> _zClasses = [];
    
    public IEnumerable<IZClassConfig> GetAll()
    {
        return _zClasses;
    }

    public IEnumerable<IZClassConfig> GetAllEnabled()
    {
        return _zClasses.Where(zClass => zClass.Enabled).ToList();
    }

    public void Register()
    {
        _zClasses.Clear();

        var rounds = config.Value.GetAll();
        
        _zClasses.AddRange(rounds);
    }
}