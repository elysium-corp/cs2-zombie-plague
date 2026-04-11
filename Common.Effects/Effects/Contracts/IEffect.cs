using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Contracts;

public interface IEffect
{
    public IPlayer? Caster { get; }
    
    public IPlayer Target { get; }
    
    public float Duration { get; }
    
    /// <summary>
    /// Немедленно удаляет эффект, игнорируя его оставшееся время действия.
    /// </summary>
    public void Destroy();

    public void Start();
}