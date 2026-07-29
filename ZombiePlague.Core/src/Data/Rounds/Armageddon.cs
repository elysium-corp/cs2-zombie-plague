using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Armageddon(
    ISwiftlyCore core,
    RoundManager roundManager,
    IZombieManager zombieManager,
    IHumanManager humanManager,
    ArmageddonConfig config) : BaseRound(core, roundManager)
{
    public override int Chance => config.Chance;
    public override string Name => "Армагеддон";

    protected override void OnStart()
    {
        Core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;

        var allPlayers = Core.PlayerManager.GetAlive().Shuffle().ToList();
        var countPlayers = allPlayers.Count;

        for (int order = 0; order < countPlayers; order++)
        {
            if (order < countPlayers / 2)
            {
                humanManager.SetSurvivor(allPlayers[order], config);
            }
            else
            {
                zombieManager.SetNemesis(allPlayers[order], config);
            }
        }

        if (config.IsMusicEnabled)
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }

        Core.PlayerManager.SendCenter(Name);
    }

    protected override void OnEnd()
    {
        Core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext @event)
        => @event.ApplyNemesisExtraDamage(config.NemesisExtraDamage);
}