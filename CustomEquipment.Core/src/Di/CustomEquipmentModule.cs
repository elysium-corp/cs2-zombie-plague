using Common.Di;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Fetcher;
using CustomEquipment.Fetcher.Analyzers;
using CustomEquipment.Giver;
using CustomEquipment.Api;
using CustomEquipment.Services;
using CustomEquipment.SharedApi;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace CustomEquipment.Di;

internal sealed class CustomEquipmentModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        AddSingleton<IEquipmentFetcher>(service, OnWeaponRegistratorFactory);
        AddSingleton<IEquipmentService, EquipmentService>(service);
        AddSingleton<IItemService, ItemService>(service);
        AddSingleton<IParticleService, ParticleService>(service);
        AddSingleton<IWeaponController, WeaponController>(service);
        AddSingleton<IParticleController, ParticleController>(service);
        AddSingleton<IItemGiver, ItemGiver>(service);
        AddSingleton<CustomEquipmentApi>(service);
        
        EventServiceRegistration(service);

        return (service.BuildServiceProvider(), service);
    }

    private void EventServiceRegistration(ServiceCollection service)
    {
        AddSingleton<EventService>(service);
        AddSingleton<IEventSubscriber>(service, sp => sp.GetRequiredService<EventService>());
        AddSingleton<IEventPublisher>(service, sp => sp.GetRequiredService<EventService>());
    }

    private IEquipmentFetcher OnWeaponRegistratorFactory(IServiceProvider service)
    {
        var compileAnalyzer = new CompileAnalyzer<IItem>();
        return new EquipmentFetcher(compileAnalyzer: compileAnalyzer);
    }
}
