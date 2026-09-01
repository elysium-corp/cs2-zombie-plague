using System.Reflection;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Localization.Api;
using Statistics.Core.Services;
using ZombiePlague.Api;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Api.Events.Contexts.Round;

namespace Statistics.Core.Tests;

public sealed class StatisticsCollectorRebindingTests
{
    [Fact]
    public void Initialize_WhenRunning_MovesSubscriptionsToReplacementApi()
    {
        var previous = new TestZombiePlagueApi();
        var replacement = new TestZombiePlagueApi();
        var localization = CreateProxy<ILocalizationApi>(
            method => throw new NotSupportedException(method.Name)
        );
        var collector = new StatisticsCollector(null!, null!, null!, null!);

        collector.Initialize(previous.Api, localization);
        MarkAsStarted(collector);
        collector.Initialize(replacement.Api, localization);

        Assert.Equal(1, previous.Infecting.UnhookCount);
        Assert.Equal(1, previous.Infected.UnhookCount);
        Assert.Equal(1, previous.RoundStarted.UnhookCount);
        Assert.Equal(1, replacement.Infecting.HookCount);
        Assert.Equal(1, replacement.Infected.HookCount);
        Assert.Equal(1, replacement.RoundStarted.HookCount);
    }

    [Fact]
    public void Initialize_WhenApiInstanceIsUnchanged_DoesNotDuplicateSubscriptions()
    {
        var bindings = new TestZombiePlagueApi();
        var firstLocalization = CreateProxy<ILocalizationApi>(
            method => throw new NotSupportedException(method.Name)
        );
        var replacementLocalization = CreateProxy<ILocalizationApi>(
            method => throw new NotSupportedException(method.Name)
        );
        var collector = new StatisticsCollector(null!, null!, null!, null!);

        collector.Initialize(bindings.Api, firstLocalization);
        MarkAsStarted(collector);
        collector.Initialize(bindings.Api, replacementLocalization);

        Assert.Equal(0, bindings.Infecting.HookCount);
        Assert.Equal(0, bindings.Infecting.UnhookCount);
        Assert.Equal(0, bindings.Infected.HookCount);
        Assert.Equal(0, bindings.Infected.UnhookCount);
        Assert.Equal(0, bindings.RoundStarted.HookCount);
        Assert.Equal(0, bindings.RoundStarted.UnhookCount);
    }

    private static void MarkAsStarted(StatisticsCollector collector)
    {
        var field = typeof(StatisticsCollector).GetField(
            "_isStarted",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(field);
        field.SetValue(collector, true);
    }

    private static TInterface CreateProxy<TInterface>(Func<MethodInfo, object?> handler)
        where TInterface : class
    {
        var proxy = DispatchProxy.Create<TInterface, InterfaceProxy>();
        ((InterfaceProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private sealed class TestZombiePlagueApi
    {
        public CountingSubscription<PlayerInfectingContext> Infecting { get; } = new();
        public CountingSubscription<PlayerInfectedContext> Infected { get; } = new();
        public CountingSubscription<RoundStartedContext> RoundStarted { get; } = new();

        public IZombiePlagueApi Api { get; }

        public TestZombiePlagueApi()
        {
            var playerEvents = CreateProxy<IZombiePlaguePlayerEvents>(method => method.Name switch
            {
                "get_Infecting" => Infecting,
                "get_Infected" => Infected,
                _ => throw new NotSupportedException(method.Name)
            });
            var roundEvents = CreateProxy<IZombiePlagueRoundEvents>(method => method.Name switch
            {
                "get_Started" => RoundStarted,
                _ => throw new NotSupportedException(method.Name)
            });
            var events = CreateProxy<IZombiePlagueEvents>(method => method.Name switch
            {
                "get_Players" => playerEvents,
                "get_Rounds" => roundEvents,
                _ => throw new NotSupportedException(method.Name)
            });

            Api = CreateProxy<IZombiePlagueApi>(method => method.Name switch
            {
                "get_Events" => events,
                _ => throw new NotSupportedException(method.Name)
            });
        }
    }

    private sealed class CountingSubscription<TContext> : IHookSubscription<TContext>
        where TContext : struct, IHookContext
    {
        public int HookCount { get; private set; }
        public int UnhookCount { get; private set; }

        public void Hook(
            HookHandler<TContext> handler,
            HookPriority priority = HookPriority.Normal
        )
        {
            HookCount++;
        }

        public void Unhook(HookHandler<TContext> handler)
        {
            UnhookCount++;
        }
    }

    public class InterfaceProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } =
            method => throw new NotSupportedException(method.Name);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return Handler(targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)));
        }
    }
}
