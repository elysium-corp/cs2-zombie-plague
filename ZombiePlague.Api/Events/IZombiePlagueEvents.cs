namespace ZombiePlague.Api.Events;

public interface IZombiePlagueEvents
{
    IZombiePlaguePreEvents Pre { get; }

    IZombiePlaguePostEvents Post { get; }
}