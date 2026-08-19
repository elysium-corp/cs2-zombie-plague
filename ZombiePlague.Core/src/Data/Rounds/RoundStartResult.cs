namespace ZombiePlague.Core.Data.Rounds;

internal enum RoundStartResult
{
    // - успешно запущен
    Started,
    // - сейчас уже не preparation
    NotPreparing,
    // - условия раунда не позволяют его запустить
    CannotStart,
    // - RoundStart.Pre отменил запуск
    Cancelled
}