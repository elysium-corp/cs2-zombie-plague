using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Ability;

namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal abstract class BasePassiveAbility(ISwiftlyCore core, IAbilityConfig config)
    : IPassiveAbility, ICooldownRestricted, IParticleRestricted, ISoundPlayable, IPresentedAbility
{
    protected IPlayer Caster { get; private set; } = null!;

    protected IPlayer? Target { get; set; }

    protected bool IsEnabled => config.Enable;

    public AbilityPresentation? Presentation { get; set; }

    public bool IsActive { get; set; }

    public float RemainingCooldown => IsActive ? Math.Max(0, Cooldown - _cooldownElapsedTime) : 0;

    public abstract float Cooldown { get; }
    private CancellationTokenSource? _cooldownToken;
    private float _cooldownElapsedTime;

    public CParticleSystem? Particle { get; set; }
    public virtual bool IsCooldownNotify => false;

    private bool _isHooked;
    protected bool IsHooked => _isHooked;

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

        _isHooked = true;
    }

    public virtual void UnHook()
    {
        _isHooked = false;
        StopCooldownTimerInternal();
        IsActive = false;
        _cooldownElapsedTime = 0f;
        DestroyParticle();
        Target = null;
    }

    public void StartCooldown()
    {
        IsActive = true;
        _cooldownElapsedTime = 0f;
        StopCooldownTimerInternal();

        CancellationTokenSource? token = null;
        token = core.Scheduler.RepeatBySeconds(TickInterval, () =>
        {
            if (!ReferenceEquals(_cooldownToken, token)) return;

            _cooldownElapsedTime += TickInterval;

            if (ShouldResetCooldown())
            {
                ResetCooldown();
            }
        });
        _cooldownToken = token;
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
        var token = _cooldownToken;
        _cooldownToken = null;
        try
        {
            token?.Cancel();
        }
        catch
        {
            // игнорируем, чтобы не падать при гонках scheduler'а
        }
    }
}
