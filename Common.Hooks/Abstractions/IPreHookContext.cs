namespace Common.Hooks.Abstractions;

public interface IPreHookContext : IHookContext
{
    bool IsCancelled { get; }

    void Cancel();
}