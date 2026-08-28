using Common.Hooks.Abstractions;

namespace Common.Hooks;

/// <summary>Синхронный обработчик hook-контекста, передаваемого по ссылке.</summary>
/// <typeparam name="TContext">Тип контекста события.</typeparam>
/// <param name="context">Контекст текущего вызова.</param>
public delegate void HookHandler<TContext>(ref TContext context) where TContext : struct, IHookContext;
