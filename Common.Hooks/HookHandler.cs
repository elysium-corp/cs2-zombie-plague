using Common.Hooks.Abstractions;

namespace Common.Hooks;

public delegate void HookHandler<TContext>(ref TContext context) where TContext : struct, IHookContext;