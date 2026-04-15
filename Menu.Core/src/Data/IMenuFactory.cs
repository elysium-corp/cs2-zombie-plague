using Menu.Api.Data.Contracts;

namespace Menu.Core.Data;

internal interface IMenuFactory
{
    TMenu? Create<TMenu>() where TMenu : class, IMenu; 
}