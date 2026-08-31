using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Ability;

namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal abstract class BaseActiveAbility(
    ISwiftlyCore core,
    IAbilityConfig config,
    Func<ILocalizationApi> localization)
    : IActiveAbility, ICooldownRestricted, IParticleRestricted, ISoundPlayable
{
    protected IPlayer Caster { get; private set; } = null!;

    protected IPlayer? Target { get; set; }

    protected bool IsEnabled => config.Enable;

    public bool IsActive { get; set; }

    public abstract KeyKind? Key { get; }

    public abstract float Cooldown { get; }
    private CancellationTokenSource? _cooldownToken;
    private float _cooldownElapsedTime;

    public virtual bool IsCooldownNotify => true;
    private const int CooldownMessageTime = 300;

    public CParticleSystem? Particle { get; set; }

    private bool _isHooked;

    private const float TickInterval = 1.0f;

    public virtual void Use()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (Cooldown > 0)
        {
            StartCooldown();
        }

        CreateParticle();

        PlaySound();
    }

    public void SetCaster(IPlayer caster)
    {
        Caster = caster;
        Hook();
    }

    public virtual void Hook()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (_isHooked)
        {
            return;
        }


        if (Key == null)
        {
            core.GameHooks.Movement.RunCommand.Pre += OnRunCommand;
        }
        else
        {
            core.Event.OnClientKeyStateChanged += OnClientKeyStateChanged;
        }

        _isHooked = true;
    }

    public virtual void UnHook()
    {
        if (_isHooked)
        {
            if (Key == null)
            {
                core.GameHooks.Movement.RunCommand.Pre -= OnRunCommand;
            }
            else
            {
                core.Event.OnClientKeyStateChanged -= OnClientKeyStateChanged;
            }

            _isHooked = false;
        }

        StopCooldownTimerInternal();
        IsActive = false;
        _cooldownElapsedTime = 0f;
        DestroyParticle();
        Target = null;
    }

    public void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent @event)
    {
        if (@event.PlayerId == Caster.PlayerID && @event.Pressed && @event.Key == Key)
        {
            OnClientButtonClickHandler(@event.PlayerId, @event.Key, @event.Pressed);
        }
    }

    private void OnRunCommand(ref RunCommandMovementPreContext context)
    {
        if (context.Params.Player.PlayerID == Caster.PlayerID)
        {
            OnRunCommandHandler(ref context);
        }
    }

    protected virtual void OnClientButtonClickHandler(int playerId, KeyKind key, bool pressed)
    {
        TryUse();
    }

    protected virtual void OnRunCommandHandler(ref RunCommandMovementPreContext context)
    {
        TryUse();
    }

    protected virtual bool CanUse() => true;

    private void TryUse()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (IsActive)
        {
            if (IsCooldownNotify)
            {
                Caster.SendMessage(
                    MessageType.Alert,
                    localization().GetForPlayerOrKey(
                        Caster,
                        "ZombiePlague.Ability.Cooldown",
                        new Dictionary<string, string>
                        {
                            ["seconds"] = Math.Ceiling(Cooldown - _cooldownElapsedTime).ToString()
                        }),
                    CooldownMessageTime);
            }

            return;
        }

        if (!CanUse())
        {
            return;
        }

        Use();
    }

    public void StartCooldown()
    {
        IsActive = true;
        _cooldownElapsedTime = 0f;
        StopCooldownTimerInternal();

        _cooldownToken = core.Scheduler.RepeatBySeconds(TickInterval, () =>
        {
            _cooldownElapsedTime += TickInterval;

            if (ShouldResetCooldown())
            {
                ResetCooldown();
            }
        });
    }

    public bool ShouldResetCooldown()
    {
        return _cooldownElapsedTime >= Cooldown;
    }

    public void ResetCooldown()
    {
        IsActive = false;
        StopCooldownTimerInternal();
        _cooldownElapsedTime = 0f;
    }

    public virtual void DestroyParticle()
    {
        try
        {
            Particle?.Despawn();
        }
        catch
        {
            // не даём визуалу ломать геймплей
        }
        finally
        {
            Particle = null;
        }
    }

    public virtual void CreateParticle()
    {
    }

    public virtual void PlaySound()
    {
    }

    private void StopCooldownTimerInternal()
    {
        try
        {
            _cooldownToken?.Cancel();
        }
        catch
        {
            // игнорируем, чтобы не падать при гонках scheduler'а
        }
        finally
        {
            _cooldownToken = null;
        }
    }
}
