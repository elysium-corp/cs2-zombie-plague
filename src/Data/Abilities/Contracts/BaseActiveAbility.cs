using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Abilities.Contracts;

public abstract class BaseActiveAbility(ISwiftlyCore core) : IActiveAbility, ICooldownRestricted, IParticleRestricted, ISoundPlayable
{
    protected IPlayer Caster { get; private set; } = null!;
    
    protected IPlayer? Target { get; set; }

    public bool IsActive { get; set; }
    
    public abstract KeyKind? Key { get; }
    
    public abstract float Cooldown { get; }
    private CancellationTokenSource? _cooldownToken;
    private float _cooldownElapsedTime;
    
    private const int CooldownMessageTime = 300;
    
    public CParticleSystem? Particle { get; set; }
    
    private bool _isHooked;
    
    private const float TickInterval = 1.0f;
    
    public virtual void Use()
    {
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

    public void Hook()
    {
        if (_isHooked)
        {
            return;
        }

        core.Event.OnClientKeyStateChanged += OnClientKeyStateChanged;
        _isHooked = true;
    }
    
    public void UnHook()
    {
        if (!_isHooked)
        {
            return;
        }

        core.Event.OnClientKeyStateChanged -= OnClientKeyStateChanged;
        _isHooked = false;
    }
    
    public void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent @event)
    {
        if (@event.PlayerId == Caster.PlayerID && @event.Pressed && @event.Key == Key)
        {
            OnClientButtonClickHandler(@event.PlayerId, @event.Key, @event.Pressed);
        }
    }

    protected virtual void OnClientButtonClickHandler(int playerId, KeyKind key, bool pressed)
    {
        TryUse();
    }
    
    protected virtual bool CanUse() => true;
    
    private void TryUse()
    {
        if (IsActive)
        {
            Caster.SendMessage(MessageType.Alert,
                $"Способность восстановится через {Cooldown - _cooldownElapsedTime} секунд", CooldownMessageTime);
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
        _cooldownToken?.Cancel();
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

    public virtual void CreateParticle() { }

    public virtual void PlaySound() { }
    
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