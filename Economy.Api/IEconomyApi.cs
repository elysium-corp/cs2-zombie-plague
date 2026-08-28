using Economy.Api.Events;
using SwiftlyS2.Shared.Players;

namespace Economy.Api;

/// <summary>Публичный API экономики сервера.</summary>
public interface IEconomyApi
{
    /// <summary>События экономики.</summary>
    public IEconomyEvents Events { get; }

    /// <summary>Возвращает текущий баланс игрока либо ноль, если счёт отсутствует.</summary>
    public int GetBalance(IPlayer player);

    /// <summary>Проверяет, загружен ли счёт и достаточно ли на нём средств.</summary>
    public bool HasEnoughMoney(IPlayer player, int amount);

    /// <summary>Начисляет игроку неотрицательную сумму с учётом лимита баланса.</summary>
    public void GiveMoney(IPlayer player, int amount);

    /// <summary>Пытается атомарно списать неотрицательную сумму.</summary>
    public bool TrySpendMoney(IPlayer player, int amount);

    /// <summary>Ключ общей регистрации API.</summary>
    public static readonly string SharedApiKey = "Economy.Api.IEconomyApi";
}
