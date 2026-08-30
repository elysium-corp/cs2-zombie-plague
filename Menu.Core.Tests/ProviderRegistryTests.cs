using System.Reflection;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Providers;
using Menu.Api.Results;
using Menu.Core.Providers;
using Menu.Core.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Tests;

public sealed class ProviderRegistryTests
{
    [Fact]
    public void NewGenerationInvalidatesStaleHandleWithoutRemovingReplacement()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);
        var oldCalls = 0;
        var currentCalls = 0;
        var oldHandle = registry.Register(Provider("economy", "1.0.0"));
        Assert.True(oldHandle.RegisterMenu(Menu(
            "store",
            _ =>
            {
                Interlocked.Increment(ref oldCalls);
                return MenuOperationResult.Succeeded;
            })).IsSuccess);

        var currentHandle = registry.Register(Provider("economy", "1.1.0"));
        Assert.True(currentHandle.RegisterMenu(Menu(
            "store",
            _ =>
            {
                Interlocked.Increment(ref currentCalls);
                return MenuOperationResult.Succeeded;
            })).IsSuccess);

        Assert.False(oldHandle.IsRegistered);
        Assert.Equal(
            MenuOperationStatus.Disposed,
            oldHandle.RegisterMenu(Menu("other", _ => MenuOperationResult.Succeeded)).Status);
        Assert.Equal(MenuOperationStatus.Disposed, oldHandle.UnregisterProvider().Status);

        var firstOpen = registry.InvokeMenu("economy", "store", Invocation(new { }));
        var secondOpen = registry.InvokeMenu("economy", "store", Invocation(new { }));

        Assert.True(firstOpen.IsSuccess);
        Assert.True(secondOpen.IsSuccess);
        Assert.Equal(0, oldCalls);
        Assert.Equal(2, currentCalls);
        Assert.True(currentHandle.IsRegistered);
        Assert.Equal(new long[] { 1, 2 }, sink.RegisteredGenerations.ToArray());
    }

    [Fact]
    public void UnloadRemovesDelegatesAndMarksCurrentGenerationOffline()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);
        var calls = 0;
        var handle = registry.Register(Provider("equipment", "2.0.0"));
        Assert.True(handle.RegisterMenu(Menu(
            "inventory",
            _ =>
            {
                Interlocked.Increment(ref calls);
                return MenuOperationResult.Succeeded;
            })).IsSuccess);
        Assert.True(registry.InvokeMenu("equipment", "inventory", Invocation(new { })).IsSuccess);

        var unloaded = handle.UnregisterProvider();
        var afterUnload = registry.InvokeMenu("equipment", "inventory", Invocation(new { }));

        Assert.True(unloaded.IsSuccess);
        Assert.False(handle.IsRegistered);
        Assert.Equal(MenuOperationStatus.ProviderOffline, afterUnload.Status);
        Assert.Equal(1, calls);
        Assert.Single(sink.OfflineGenerations);
        Assert.Equal(sink.RegisteredGenerations.Single(), sink.OfflineGenerations.Single());
        Assert.Equal(MenuOperationStatus.Disposed, handle.UnregisterProvider().Status);
    }

    [Fact]
    public void InvokeAction_ValidatesArgumentsBeforeCallingHandler()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        var handlerCalls = 0;
        var handle = registry.Register(Provider("economy", "1.0.0"));
        var registered = handle.RegisterAction(new MenuProviderActionDescriptor
        {
            ActionKey = "purchase",
            DisplayName = TestReleaseFactory.Text("Purchase"),
            Validator = arguments =>
                arguments.TryGetProperty("allowed", out var allowed) && allowed.GetBoolean()
                    ? MenuValidationResult.Valid
                    : MenuValidationResult.Invalid("purchase.denied", "Purchase is denied."),
            Handler = _ =>
            {
                Interlocked.Increment(ref handlerCalls);
                return MenuOperationResult.Succeeded;
            }
        });

        var rejected = registry.InvokeAction(
            "economy",
            "purchase",
            Invocation(new { allowed = false }));
        var accepted = registry.InvokeAction(
            "economy",
            "purchase",
            Invocation(new { allowed = true }));

        Assert.True(registered.IsSuccess);
        Assert.Equal(MenuOperationStatus.ValidationFailed, rejected.Status);
        Assert.Contains(rejected.Issues, issue => issue.Code == "purchase.denied");
        Assert.True(accepted.IsSuccess);
        Assert.Equal(1, handlerCalls);
    }

    [Fact]
    public void RegisterAction_RequiresBothValidatorAndHandler()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        var handle = registry.Register(Provider("economy", "1.0.0"));

        var missingValidator = handle.RegisterAction(new MenuProviderActionDescriptor
        {
            ActionKey = "purchase",
            DisplayName = TestReleaseFactory.Text("Purchase"),
            Validator = null!,
            Handler = _ => MenuOperationResult.Succeeded
        });
        var missingHandler = handle.RegisterAction(new MenuProviderActionDescriptor
        {
            ActionKey = "refund",
            DisplayName = TestReleaseFactory.Text("Refund"),
            Validator = _ => MenuValidationResult.Valid,
            Handler = null!
        });

        Assert.Equal(MenuOperationStatus.InvalidRequest, missingValidator.Status);
        Assert.Equal(MenuOperationStatus.InvalidRequest, missingHandler.Status);
        Assert.False(registry.IsActionAvailable("economy", "purchase"));
        Assert.False(registry.IsActionAvailable("economy", "refund"));
    }

    [Fact]
    public void InvokeAction_ValidatesRegisteredSchemaBeforeProviderCallback()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        var validatorCalls = 0;
        var handlerCalls = 0;
        var handle = registry.Register(Provider("equipment", "1.0.0"));
        using (var schema = System.Text.Json.JsonDocument.Parse(
                   """{"type":"object","additionalProperties":false,"required":["weapon"],"properties":{"weapon":{"type":"string","minLength":1}}}"""))
        {
            Assert.True(handle.RegisterAction(new MenuProviderActionDescriptor
            {
                ActionKey = "select_weapon",
                DisplayName = TestReleaseFactory.Text("Select weapon"),
                ArgumentsSchema = schema.RootElement,
                Validator = _ =>
                {
                    Interlocked.Increment(ref validatorCalls);
                    return MenuValidationResult.Valid;
                },
                Handler = _ =>
                {
                    Interlocked.Increment(ref handlerCalls);
                    return MenuOperationResult.Succeeded;
                },
            }).IsSuccess);
        }

        var rejected = registry.InvokeAction("equipment", "select_weapon", Invocation(new { weapon = 42 }));
        var accepted = registry.InvokeAction("equipment", "select_weapon", Invocation(new { weapon = "ak47" }));

        Assert.Equal(MenuOperationStatus.ValidationFailed, rejected.Status);
        Assert.Contains(rejected.Issues, issue => issue.Code == "provider.argument_type");
        Assert.Equal(1, validatorCalls);
        Assert.Equal(1, handlerCalls);
        Assert.True(accepted.IsSuccess);
    }

    [Fact]
    public void ProviderHandler_CanUnregisterItsOwnSessionWithoutLockRecursion()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);
        IMenuProviderRegistration? handle = null;
        handle = registry.Register(Provider("self_unregister", "1.0.0"));
        Assert.True(handle.RegisterMenu(Menu(
            "main",
            _ => handle.UnregisterProvider())).IsSuccess);

        var result = registry.InvokeMenu("self_unregister", "main", Invocation(new { }));

        Assert.True(result.IsSuccess);
        Assert.False(handle.IsRegistered);
        Assert.Single(sink.OfflineGenerations);
        Assert.Equal(MenuOperationStatus.ProviderOffline,
            registry.InvokeMenu("self_unregister", "main", Invocation(new { })).Status);
    }

    [Fact]
    public void Unload_WaitsForHandlerAlreadyInFlight()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var unloadStarted = new ManualResetEventSlim();
        var handle = registry.Register(Provider("slow_provider", "1.0.0"));
        Assert.True(handle.RegisterMenu(Menu(
            "main",
            _ =>
            {
                entered.Set();
                release.Wait();
                return MenuOperationResult.Succeeded;
            })).IsSuccess);

        var invocation = Task.Run(() =>
            registry.InvokeMenu("slow_provider", "main", Invocation(new { })));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
        var unload = Task.Run(() =>
        {
            unloadStarted.Set();
            return handle.UnregisterProvider();
        });
        Assert.True(unloadStarted.Wait(TimeSpan.FromSeconds(2)));

        try
        {
            Assert.False(unload.Wait(TimeSpan.FromMilliseconds(100)));
        }
        finally
        {
            release.Set();
        }

        Assert.True(invocation.GetAwaiter().GetResult().IsSuccess);
        Assert.True(unload.GetAwaiter().GetResult().IsSuccess);
    }

    [Fact]
    public void ValidationCatalog_DoesNotRetainCallableDelegateAfterUnload()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        var validatorCalls = 0;
        var handle = registry.Register(Provider("catalog_provider", "1.0.0"));
        Assert.True(handle.RegisterAction(new MenuProviderActionDescriptor
        {
            ActionKey = "validate",
            DisplayName = TestReleaseFactory.Text("Validate"),
            Validator = _ =>
            {
                Interlocked.Increment(ref validatorCalls);
                return MenuValidationResult.Valid;
            },
            Handler = _ => MenuOperationResult.Succeeded
        }).IsSuccess);
        var catalog = registry.BuildValidationCatalog();
        Assert.True(catalog.TryGet("catalog_provider", out var provider));
        var validator = provider.ArgumentValidators["validate"];

        Assert.True(handle.UnregisterProvider().IsSuccess);
        var result = validator(TestReleaseFactory.Json(new { }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "provider_offline");
        Assert.Equal(0, validatorCalls);
    }

    [Fact]
    public void Stop_IsIdempotentAndRejectsLaterRegistrations()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);
        var handle = registry.Register(Provider("active_provider", "1.0.0"));
        var incompatible = registry.Register(Provider("incompatible_provider", "1.0.0") with
        {
            MenuApiVersion = MenuContractVersions.MenuCoreApiVersion + 1
        });

        registry.Stop();
        registry.Stop();
        var rejected = registry.Register(Provider("late_provider", "1.0.0"));

        Assert.False(handle.IsRegistered);
        Assert.False(incompatible.IsRegistered);
        Assert.Equal(2, sink.OfflineGenerations.Count);
        Assert.Contains(1, sink.OfflineGenerations);
        Assert.Contains(2, sink.OfflineGenerations);
        Assert.False(rejected.IsRegistered);
        Assert.Equal(MenuOperationStatus.Disposed, rejected.RegistrationResult.Status);
        Assert.Equal("provider_registry_stopped", rejected.RegistrationResult.Code);
    }

    [Fact]
    public void Stop_DoesNotHoldLifecycleLockWhileWaitingForProviderCallback()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        using var entered = new ManualResetEventSlim();
        using var continueCallback = new ManualResetEventSlim();
        var handle = registry.Register(Provider("callback_provider", "1.0.0"));
        Assert.True(handle.RegisterMenu(Menu(
            "main",
            _ =>
            {
                entered.Set();
                continueCallback.Wait();
                var late = registry.Register(Provider("from_callback", "1.0.0"));
                return late.RegistrationResult.Status == MenuOperationStatus.Disposed
                    ? MenuOperationResult.Succeeded
                    : MenuOperationResult.Failure(MenuOperationStatus.HandlerFailed, "late_registration_accepted");
            })).IsSuccess);

        var invocation = Task.Run(() =>
            registry.InvokeMenu("callback_provider", "main", Invocation(new { })));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
        var stopping = Task.Run(registry.Stop);
        Assert.True(SpinWait.SpinUntil(() => !handle.IsRegistered, TimeSpan.FromSeconds(2)));

        try
        {
            continueCallback.Set();
            Assert.True(stopping.Wait(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            continueCallback.Set();
        }

        Assert.True(invocation.GetAwaiter().GetResult().IsSuccess);
    }

    [Fact]
    public void IncompatibleRegistration_IsPersistedAndInvalidatesPreviousGeneration()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);
        var previous = registry.Register(Provider("versioned_provider", "1.0.0"));
        var incompatible = registry.Register(Provider("versioned_provider", "2.0.0") with
        {
            MenuApiVersion = MenuContractVersions.MenuCoreApiVersion + 1
        });

        Assert.False(previous.IsRegistered);
        Assert.False(incompatible.IsRegistered);
        Assert.Equal(MenuOperationStatus.Unsupported, incompatible.RegistrationResult.Status);
        Assert.Equal("provider_api_incompatible", incompatible.RegistrationResult.Code);
        Assert.False(registry.IsProviderOnline("versioned_provider"));
        Assert.True(registry.BuildValidationCatalog().TryGet("versioned_provider", out var validationEntry));
        Assert.Equal(ProviderAvailability.Incompatible, validationEntry.Availability);
        var rejected = Assert.Single(sink.RejectedRegistrations);
        Assert.Equal(ProviderRejectionStatus.Incompatible, rejected.Status);
        Assert.Equal(2, rejected.Generation);

        incompatible.Dispose();

        Assert.Contains(2, sink.OfflineGenerations);
    }

    [Fact]
    public void PersistenceColumnLimits_AreRejectedBeforeRegistryMutation()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);

        var longName = registry.Register(Provider("long_name", "1.0.0") with
        {
            DisplayName = new string('x', 129)
        });
        var longVersion = registry.Register(Provider("long_version", new string('1', 33)));

        Assert.Equal(MenuOperationStatus.InvalidIdentifier, longName.RegistrationResult.Status);
        Assert.Equal(MenuOperationStatus.InvalidIdentifier, longVersion.RegistrationResult.Status);
        Assert.Empty(sink.RegisteredGenerations);
        Assert.Empty(sink.RejectedRegistrations);
    }

    [Fact]
    public void StaleRejectedHandle_CannotMarkCompatibleReplacementOffline()
    {
        var sink = new RecordingProviderStateSink();
        var registry = CreateRegistry(sink);
        var rejected = registry.Register(Provider("recovering_provider", "2.0.0") with
        {
            MenuApiVersion = MenuContractVersions.MenuCoreApiVersion + 1
        });
        var replacement = registry.Register(Provider("recovering_provider", "2.0.1"));

        rejected.Dispose();

        Assert.True(replacement.IsRegistered);
        Assert.True(registry.IsProviderOnline("recovering_provider"));
        Assert.Empty(sink.OfflineGenerations);
        Assert.Equal(new long[] { 2 }, sink.RegisteredGenerations);
    }

    [Fact]
    public void ExportDisplayNames_RespectPersistenceColumnLimit()
    {
        var registry = CreateRegistry(new RecordingProviderStateSink());
        var handle = registry.Register(Provider("bounded_exports", "1.0.0"));
        var longText = TestReleaseFactory.Text(new string('x', 129));

        var menu = handle.RegisterMenu(new MenuProviderMenuDescriptor
        {
            MenuKey = "menu",
            DisplayName = longText,
            Handler = _ => MenuOperationResult.Succeeded
        });
        var action = handle.RegisterAction(new MenuProviderActionDescriptor
        {
            ActionKey = "action",
            DisplayName = longText,
            Validator = _ => MenuValidationResult.Valid,
            Handler = _ => MenuOperationResult.Succeeded
        });

        Assert.Equal(MenuOperationStatus.InvalidRequest, menu.Status);
        Assert.Equal(MenuOperationStatus.InvalidRequest, action.Status);
        Assert.False(registry.IsMenuAvailable("bounded_exports", "menu"));
        Assert.False(registry.IsActionAvailable("bounded_exports", "action"));
    }

    private static ProviderRegistry CreateRegistry(IProviderStateSink sink)
    {
        return new ProviderRegistry(sink, NullLogger<ProviderRegistry>.Instance);
    }

    private static MenuProviderDescriptor Provider(string providerKey, string version)
    {
        return new MenuProviderDescriptor
        {
            ProviderKey = providerKey,
            DisplayName = providerKey,
            PluginVersion = version,
            MenuApiVersion = MenuContractVersions.MenuCoreApiVersion
        };
    }

    private static MenuProviderMenuDescriptor Menu(string menuKey, MenuProviderMenuHandler handler)
    {
        return new MenuProviderMenuDescriptor
        {
            MenuKey = menuKey,
            DisplayName = TestReleaseFactory.Text(menuKey),
            Handler = handler
        };
    }

    private static MenuProviderInvocationContext Invocation(object arguments)
    {
        var player = DispatchProxy.Create<IPlayer, TestPlayerProxy>();
        return new MenuProviderInvocationContext(
            player,
            player,
            TestReleaseFactory.Json(arguments),
            0);
    }

    public class TestPlayerProxy : DispatchProxy
    {
        public TestPlayerProxy()
        {
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new InvalidOperationException("Игровой IPlayer не должен вызываться в domain-тесте registry.");
        }
    }

    private sealed class RecordingProviderStateSink : IProviderStateSink
    {
        internal List<long> RegisteredGenerations { get; } = [];

        internal List<long> OfflineGenerations { get; } = [];

        internal List<(long Generation, ProviderRejectionStatus Status)> RejectedRegistrations { get; } = [];

        public void ProviderRegistered(MenuProviderDescriptor descriptor, Guid sessionId, long generation)
        {
            RegisteredGenerations.Add(generation);
        }

        public void ProviderRejected(
            MenuProviderDescriptor descriptor,
            Guid sessionId,
            long generation,
            ProviderRejectionStatus status,
            string errorCode)
        {
            RejectedRegistrations.Add((generation, status));
        }

        public void MenuDeclared(
            string providerKey,
            Guid sessionId,
            long generation,
            MenuProviderMenuDescriptor descriptor)
        {
        }

        public void ActionDeclared(
            string providerKey,
            Guid sessionId,
            long generation,
            MenuProviderActionDescriptor descriptor)
        {
        }

        public void ExportRemoved(
            string providerKey,
            Guid sessionId,
            long generation,
            string exportType,
            string exportKey)
        {
        }

        public void ProviderOffline(string providerKey, Guid sessionId, long generation)
        {
            OfflineGenerations.Add(generation);
        }
    }
}
