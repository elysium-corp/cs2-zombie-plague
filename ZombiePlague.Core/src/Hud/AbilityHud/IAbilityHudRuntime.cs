namespace ZombiePlague.Core.Hud.AbilityHud;

/// <summary>Ресурсы одной панели HUD, принадлежащие текущему запуску сервиса</summary>
internal interface IAbilityHudRuntime : IAbilityHudSink, IDisposable
{
    /// <summary>Сущность существует и допускает обновление персонального состояния на игровом потоке</summary>
    bool IsValid { get; }
}
