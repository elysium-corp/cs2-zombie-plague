using CS2ZombiePlague.Data.Effects;
using CS2ZombiePlague.Data.Effects.Contracts;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Managers;

public class EffectManager(ISwiftlyCore core)
{
    private readonly IEffectFactory _effectFactory = DependencyManager.GetService<IEffectFactory>();
    
    public readonly List<BaseEffect> Effects = new();
    
    public void RegisterHooks()
    {
        core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);
    }

    public HookResult PlayerChatEvent(EventPlayerChat @event)
    {
        if (@event.Text == "123")
        {
            ApplyEffect<Burn>(null, @event.UserIdPlayer);
        }
        
        return HookResult.Continue;
    }
    public BaseEffect ApplyEffect<T>(IPlayer? caster, IPlayer target) where T : BaseEffect
    {
        return _effectFactory.Create<T>(caster, target);
    }
    
    public bool PlayerHasEffect<T>(IPlayer player) where T : BaseEffect
    {
        return  Effects.Find(ef => ef is T && ef.Target.Equals(player)) != null;
    }
}