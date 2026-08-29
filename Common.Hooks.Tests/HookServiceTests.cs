using Common.Hooks.Abstractions;
using Xunit;

namespace Common.Hooks.Tests;

public sealed class HookServiceTests
{
    [Fact]
    public void DispatchCancellable_InvokesRemainingHandlers_AfterCancellation()
    {
        var hooks = new HookService();
        var calls = new List<string>();

        hooks.Hook<TestPreContext>((ref TestPreContext context) =>
        {
            calls.Add("cancel");
            context.Cancel();
        }, HookPriority.High);

        hooks.Hook<TestPreContext>((ref TestPreContext context) =>
        {
            calls.Add("remaining");
        }, HookPriority.Low);

        var context = new TestPreContext();

        var accepted = hooks.DispatchCancellable(ref context);

        Assert.False(accepted);
        Assert.True(context.IsCancelled);
        Assert.Equal(new[] { "cancel", "remaining" }, calls);
    }

    [Fact]
    public void Dispatch_UsesPriorityThenRegistrationOrder()
    {
        var hooks = new HookService();
        var calls = new List<string>();

        hooks.Hook<TestPostContext>((ref TestPostContext context) => calls.Add("normal-first"));
        hooks.Hook<TestPostContext>((ref TestPostContext context) => calls.Add("high"), HookPriority.High);
        hooks.Hook<TestPostContext>((ref TestPostContext context) => calls.Add("normal-second"));

        var context = new TestPostContext();

        hooks.Dispatch(ref context);

        Assert.Equal(new[] { "high", "normal-first", "normal-second" }, calls);
    }

    [Fact]
    public void Dispatch_UsesStableSnapshot_WhenHandlerUnhooksItself()
    {
        var hooks = new HookService();
        var calls = new List<string>();

        HookHandler<TestPostContext>? selfRemoving = null;
        selfRemoving = (ref TestPostContext context) =>
        {
            calls.Add("self");
            hooks.Unhook(selfRemoving!);
        };

        hooks.Hook(selfRemoving!);
        hooks.Hook<TestPostContext>((ref TestPostContext context) => calls.Add("remaining"));

        var first = new TestPostContext();
        hooks.Dispatch(ref first);

        var second = new TestPostContext();
        hooks.Dispatch(ref second);

        Assert.Equal(new[] { "self", "remaining", "remaining" }, calls);
    }

    [Fact]
    public void Dispatch_ContinuesAfterHandlerException()
    {
        var failures = new List<Exception>();
        var hooks = new HookService((exception, _, _) => failures.Add(exception));
        var called = false;

        hooks.Hook<TestPostContext>((ref TestPostContext context) => throw new InvalidOperationException("boom"));
        hooks.Hook<TestPostContext>((ref TestPostContext context) => called = true);

        var context = new TestPostContext();
        hooks.Dispatch(ref context);

        Assert.True(called);
        Assert.Single(failures);
        Assert.IsType<InvalidOperationException>(failures[0]);
    }

    [Fact]
    public void Dispatch_ContinuesWhenExceptionHandlerAlsoThrows()
    {
        var hooks = new HookService((_, _, _) => throw new InvalidOperationException("diagnostics"));
        var called = false;
        hooks.Hook<TestPostContext>((ref TestPostContext _) => throw new InvalidOperationException("subscriber"));
        hooks.Hook<TestPostContext>((ref TestPostContext _) => called = true);
        var context = new TestPostContext();
        hooks.Dispatch(ref context);
        Assert.True(called);
    }

    private struct TestPreContext : IPreHookContext
    {
        public bool IsCancelled { get; private set; }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }

    private struct TestPostContext : IPostHookContext
    {
    }
}
