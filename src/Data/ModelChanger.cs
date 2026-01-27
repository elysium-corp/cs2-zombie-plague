using CS2ZombiePlague.Config.models;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data;

public class ModelChanger(
    ISwiftlyCore core,
    ZombieManager zombieManager,
    RoundManager roundManager,
    CommonUtils utils,
    IOptions<ModelsConfig> modelsConfig)
{
    private static readonly HashSet<string> RadioArray = new(StringComparer.OrdinalIgnoreCase)
    {
        "coverme", "takepoint", "holdpos", "regroup", "followme", "takingfire",
        "go", "fallback", "sticktog", "getinpos", "stormfront", "report", "roger",
        "enemyspot", "needbackup", "sectorclear", "inposition", "reportingin",
        "getout", "negative", "enemydown", "sorry", "cheer", "compliment",
        "thanks", "go_a", "go_b", "needrop", "deathcry", "radio", "radio1",
        "radio2", "radio3"
    };

    private readonly Dictionary<int, List<string>> _playersModel = new();
    private readonly Dictionary<int, string> _currentModel = new();

    private List<string> _defaultHumanModelPaths = new();

    public void Load()
    {
        _defaultHumanModelPaths = modelsConfig.Value.DefaultHumanModels;

        core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);


        if (modelsConfig.Value.EnableRadioCommands)
        {
            core.Command.HookClientCommand((playerId, commandLine) =>
            {
                if (string.IsNullOrWhiteSpace(commandLine))
                    return HookResult.Continue;

                var commandName = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                var player = core.PlayerManager.GetPlayer(playerId);

                if (player == null || !RadioArray.Contains(commandName) || !HasCustomModels(player))
                    return HookResult.Continue;

                if (!_currentModel.TryGetValue(player.PlayerID, out var modelPath))
                    return HookResult.Continue;

                if (player.PlayerPawn.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName !=
                    _currentModel[playerId])
                {
                    return HookResult.Continue;
                }

                var modelConfig = GetModelByModelPath(modelPath);
                if (modelConfig is not { RadioCommandIsEnabled: true })
                    return HookResult.Continue;

                if (modelConfig.RadioCommands.TryGetValue(commandName, out var sound))
                {
                    PlaySound(player, sound);
                    return HookResult.Stop;
                }

                return HookResult.Continue;
            });
        }
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var models = GetPlayerModelsFromDb(@event.UserIdPlayer);
        if (models != null)
        {
            _playersModel[@event.UserId] = models;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (HasCustomModels(@event.UserIdPlayer))
        {
            _playersModel.Remove(@event.UserId);
            _currentModel.Remove(@event.UserId);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        var allPlayers = core.PlayerManager.GetAlive();

        foreach (var player in allPlayers)
        {
            ApplyPlayerModel(player);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        if (player.IsInfected() && !roundManager.IsNoneRound())
        {
            var zombieModel = zombieManager.GetZombie(player.PlayerID)?.GetZombieClass().Model;
            if (zombieModel != null)
            {
                player.SetModel(zombieModel);
            }

            return HookResult.Continue;
        }

        var pawn = player.Pawn;
        if (pawn is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        ApplyPlayerModel(player);

        return HookResult.Continue;
    }

    private HookResult PlayerChatEvent(EventPlayerChat @event)
    {
        var commandLine = @event.Text;
        if (commandLine != "!model")
        {
            return HookResult.Continue;
        }

        var player = core.PlayerManager.GetPlayer(@event.Playerid);
        if (player != null)
        {
            ShowMenu(player);
        }

        return HookResult.Continue;
    }

    private bool HasCustomModels(IPlayer player)
    {
        return _playersModel.ContainsKey(player.PlayerID);
    }

    private List<string>? GetPlayerModelsFromDb(IPlayer player)
    {
        return ["messi"];
    }

    private void SetModel(CBasePlayerPawn pawn, string modelPath)
    {
        core.Scheduler.NextTick(() => { pawn.SetModel(modelPath); });
    }

    private void ShowMenu(IPlayer player)
    {
        var menu = CreateMenu(player);
        core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    private IMenuAPI CreateMenu(IPlayer player)
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Выбери модель")
            .EnableSound()
            .Build();

        if (HasCustomModels(player))
        {
            AddModelsToMenu(menu, player);
        }

        return menu;
    }

    private void AddModelsToMenu(IMenuAPI menu, IPlayer player)
    {
        var removeModelButton = new ButtonMenuOption("Убрать модель");
        removeModelButton.Click += async (_, args) =>
        {
            if (_currentModel.ContainsKey(player.PlayerID))
            {
                _currentModel[player.PlayerID] =
                    _defaultHumanModelPaths[utils.RandomNum(0, _defaultHumanModelPaths.Count)];
                @args.Player.SendChatAsync("Модель будет убрана в следующем раунде!");
            }
        };
        menu.AddOption(removeModelButton);

        var modelsIternalName = _playersModel[player.PlayerID];
        foreach (var name in modelsIternalName)
        {
            var modelConfig = GetModelByIternalName(name);
            if (modelConfig is not null)
            {
                var button = new ButtonMenuOption(modelConfig.InternalName);
                button.Click += async (_, args) =>
                {
                    _currentModel[player.PlayerID] = modelConfig.ModelPath;
                    @args.Player.SendChatAsync("Модель будет установлена в следующем раунде!");
                };
                menu.AddOption(button);
            }
        }
    }

    private IModelConfig? GetModelByIternalName(string modelName)
    {
        return modelsConfig.Value.Models.Find(model => model.InternalName == modelName);
    }

    private IModelConfig? GetModelByModelPath(string modelPath)
    {
        return modelsConfig.Value.Models.Find(model => model.ModelPath == modelPath);
    }

    private void PlaySound(IPlayer player, string soundName)
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 0.5f,
            Name = soundName,
            SourceEntityIndex = (int)player.PlayerPawn.Index
        };
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();
    }

    private void ApplyPlayerModel(IPlayer player)
    {
        var pawn = player.PlayerPawn;
        if (pawn is not { IsValid: true })
            return;

        if (!HasCustomModels(player))
        {
            var model = _defaultHumanModelPaths[utils.RandomNum(0, _defaultHumanModelPaths.Count)];
            SetModel(pawn, model);
            return;
        }

        if (_currentModel.TryGetValue(player.PlayerID, out var modelPath) &&
            !string.IsNullOrWhiteSpace(modelPath))
        {
            SetModel(pawn, modelPath);
        }
    }
}