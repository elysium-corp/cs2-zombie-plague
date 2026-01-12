using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Abilities.Contracts;

public abstract class BasePassiveAbility(ISwiftlyCore core) : IPassiveAbility, ICooldownRestricted, IParticleRestricted, ISoundPlayable
{
    protected IPlayer Caster { get; private set; } = null!;
    
    protected IPlayer? Target { get; set; }
    
    public bool IsActive { get; set; }
    
    public abstract float Cooldown { get; }
    private CancellationTokenSource? _cooldownToken;
    private float _cooldownElapsedTime;
    
    public CParticleSystem? Particle { get; set; }
    
    private bool _isHooked;
    
    private const float TickInterval = 1.0f;

    public virtual void Use()
    {
        if (Cooldown > 0)
        {
            StartCooldown();
        }

        if (Particle != null)
        {
            CreateParticle();
        }
    }

    public void SetCaster(IPlayer caster)
    {
        Caster = caster;
        Hook();
    }

    public virtual void Hook()
    {
        if (_isHooked)
        {
            return;
        }
        
        _isHooked = true;
    }

    public virtual void UnHook()
    {
        if (!_isHooked)
        {
            return;
        }
        
        _isHooked = false;
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