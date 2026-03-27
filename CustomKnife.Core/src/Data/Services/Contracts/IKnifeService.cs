using CustomKnife.Data.Models;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Services.Contracts;

public interface IKnifeService
{
    /// <summary>
    /// Пытается выдать нож игроку.
    /// </summary>
    public bool TryGiveKnife(IPlayer? player);
    
    /// <summary>
    /// Пытается применить бонусы ножа к игроку.
    /// </summary>
    public bool TryApplyProperties(IPlayer? player);
    
    /// <summary>
    /// Пытается нанести дополнительный урон жертве.
    /// </summary>
    public bool TryApplyKnifeDamage(IOnEntityTakeDamageEvent @event);
    
    /// <summary>
    /// Пытается применить отдачу к жертве.
    /// </summary>
    public bool TryApplyKnifeKnockback(EventPlayerHurt @event);
    
    /// <summary>
    /// Получает текущий нож игрока.
    /// </summary>
    public IKnife GetKnife(IPlayer player);
    
    /// <summary>
    /// Получает список всех зарегистрированных ножей на сервере.
    /// </summary>
    public List<IKnife> GetRegisteredKnives();
    
    /// <summary>
    /// Меняет текущий нож игрока на новый и затем пытается его выдать.
    /// </summary>
    public void ChangeKnife(IPlayer player, IKnife knife);
}