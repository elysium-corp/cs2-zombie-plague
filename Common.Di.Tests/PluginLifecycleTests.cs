using Common.Di.SharedInterfaces;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;
using Xunit;

namespace Common.Di.Tests;

public sealed class PluginLifecycleTests
{
    [Fact]
    public void Bind_WhenInterfaceIsRebuilt_UsesNewestInstance()
    {
        var reference = new SharedInterfaceReference<ITestApi>();
        var first = new TestApi("first");
        var second = new TestApi("second");

        reference.Bind(first);
        reference.Bind(second);

        Assert.Same(second, reference.Value);
    }

    [Fact]
    public void OnSharedInterfaceInjected_WhenRepeated_RunsReadyOnlyOnce()
    {
        var plugin = new TestPlugin();

        plugin.OnSharedInterfaceInjected(null!);
        plugin.OnSharedInterfaceInjected(null!);

        Assert.Equal(2, plugin.InjectedCount);
        Assert.Equal(1, plugin.ReadyCount);
    }

    [Fact]
    public void OnSharedInterfaceInjected_WhenConcurrent_RunsReadyOnlyOnce()
    {
        var plugin = new TestPlugin();

        Parallel.For(0, 32, _ => plugin.OnSharedInterfaceInjected(null!));

        Assert.Equal(32, plugin.InjectedCount);
        Assert.Equal(1, plugin.ReadyCount);
    }

    private interface ITestApi
    {
    }

    private sealed record TestApi(string Name) : ITestApi;

    private sealed class TestPlugin() : Plugin<TestModule>(null!)
    {
        private int _injectedCount;
        private int _readyCount;

        public int InjectedCount => Volatile.Read(ref _injectedCount);

        public int ReadyCount => Volatile.Read(ref _readyCount);

        protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
        {
            Interlocked.Increment(ref _injectedCount);
        }

        protected override void OnReady()
        {
            Interlocked.Increment(ref _readyCount);
        }
    }

    private sealed class TestModule : IModule
    {
        public (ServiceProvider, ServiceCollection) GetProvider()
        {
            throw new NotSupportedException();
        }
    }
}
