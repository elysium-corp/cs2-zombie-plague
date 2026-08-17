namespace Common.Hooks.Abstractions;

public interface IHookPublisher
{
    void Dispatch<TContext>(ref TContext context) where TContext : struct, IHookContext;
}