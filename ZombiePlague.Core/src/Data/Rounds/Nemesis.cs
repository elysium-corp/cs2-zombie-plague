using Common.Hooks.Abstractions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Nemesis(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IHookPublisher hooks,
    NemesisConfig config
) : RoundBase(core, playerManager, hooks)
{
    public override string Name => config.Name;
    
    protected override void OnStart()
    {
        var humans = PlayerManager.GetAllAliveHumans().ToArray();
        var humansCount = humans.Length;
        var selectedPlayer = humans[Random.Shared.Next(humans.Length)];

        if (!PlayerManager.TrySetNemesis(selectedPlayer, out var nemesis))
        {
            return;
        }
        
        var health = nemesis.ZClass.Health + config.NemesisBonusHealthPerPlayer * humansCount;

        nemesis.ZClass.Health = Math.Clamp(
            health,
            min: nemesis.ZClass.Health,
            max: int.MaxValue
        );
        
        if (config.IsMusicEnabled)
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }
        
        if (!config.NemesisLeap)
        {
            var leap = nemesis.ZClass.Abilities.OfType<Leap>().FirstOrDefault();
            leap?.UnHook();
        }

        Core.PlayerManager.SendCenter($"Немезида => {selectedPlayer.Name}");
    }

    protected override void OnEnd() { }
    
    public override bool CanStart()
    {
        var humansCount = PlayerManager.GetAllAliveHumans().Count();
        
        return humansCount > config.MinimumHumansRequired;
    }

    protected override void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();
        
        if (attacker is not { IsValid: true, IsAlive: true } || !PlayerManager.IsNemesis(attacker))
        {
            return;
        }

        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();
        
        if (victim is not { IsValid: true, IsAlive: true } || !PlayerManager.IsHuman(victim))
        {
            return;
        }

        context.Params.Info.Damage += config.NemesisExtraDamage;
    }
}