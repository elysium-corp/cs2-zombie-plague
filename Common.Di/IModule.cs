using Microsoft.Extensions.DependencyInjection;

namespace Common.Di;

public interface IModule
{
    public ServiceProvider GetProvider();
}