using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Data.Contracts;

public abstract class MenuBase(ISwiftlyCore core) : IMenu
{
    protected ISwiftlyCore Core => core;
    
    protected virtual MenuTeamAccess AllowedTeams => MenuTeamAccess.All;
    
    protected virtual IReadOnlyCollection<string> Commands => [];
    
    protected virtual bool RegisterCommandsAsRaw => true;
    
    private readonly List<Guid> _commandHooks = [];

    public abstract string Id { get; }

    public void Open(IPlayer player)
    {
        if (!player.IsValid || !CanOpen(player))
        {
            return;
        }

        Core.MenusAPI.OpenMenuForPlayer(player, Build(player));
    }
    
    protected virtual bool CanOpenCore(IPlayer player)
    {
        return true;
    }
    
    public void RegisterCommands()
    {
        foreach (var command in Commands.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var hook = Core.Command.RegisterCommand(
                commandName: command,
                handler: OnCommand,
                registerRaw: RegisterCommandsAsRaw
            );

            _commandHooks.Add(hook);
        }
    }
    
    public void UnregisterCommands()
    {
        foreach (var hook in _commandHooks)
        {
            Core.Command.UnregisterCommand(hook);
        }

        _commandHooks.Clear();
    }

    protected abstract IMenuAPI Build(IPlayer player);

    protected IMenuBuilderAPI CreateBuilder(IPlayer player)
    {
        var builder = Core.MenusAPI.CreateBuilder();

        return ConfigureDesign(
            player,
            builder.Design
        );
    }

    protected abstract IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design);
    
    private bool HasTeamAccess(IPlayer player)
    {
        var requiredAccess = player.Controller.Team switch
        {
            Team.T => MenuTeamAccess.T,
            Team.CT => MenuTeamAccess.CT,
            Team.Spectator => MenuTeamAccess.Spectator,
            _ => MenuTeamAccess.None
        };

        return (AllowedTeams & requiredAccess) != 0;
    }
    
    private bool CanOpen(IPlayer player)
    {
        return HasTeamAccess(player)
               && CanOpenCore(player);
    }
    
    private void OnCommand(ICommandContext context)
    {
        if (context.Sender is not { IsValid: true } player)
        {
            return;
        }

        Open(player);
    }
}