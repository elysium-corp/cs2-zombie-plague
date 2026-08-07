using Menu.Api.Data.Contracts;

namespace Menu.Api.Data.Factory;

public interface IMenuFactory
{
    TMenu? Create<TMenu>() where TMenu : class, IMenu; 
}