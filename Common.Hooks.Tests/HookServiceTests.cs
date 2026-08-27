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
