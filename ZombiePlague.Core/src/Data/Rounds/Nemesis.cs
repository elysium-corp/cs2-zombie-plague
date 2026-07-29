using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Nemesis(
    ISwiftlyCore core,
    IZombieManager zombieManager,
    NemesisConfig config) : BaseRound(core)
{
    public override int Chance => config.Chance;
    public override string Name => "Немезида";

    protected override void OnStart()
    {
        Core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;

        var players = Core.PlayerManager.GetAlive().ToList();
        var nemesis = players[Random.Shared.Next(0, players.Count)];

        zombieManager.SetNemesis(nemesis, config);

        foreach (var player in players)
        {
            if (zombieManager.GetZombie(player) == null)
            {
                player.SwitchTeam(Team.CT);
            }
        }

        if (config.IsMusicEnabled)
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }

        Core.PlayerManager.SendCenter("Немезида => " + nemesis.Name);
    }

    protected override void OnEnd()
    {
        Core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext @event)
        => @event.ApplyNemesisExtraDamage(config.NemesisExtraDamage, zombieManager, core);
}
