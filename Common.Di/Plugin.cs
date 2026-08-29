using Common.Di.Exceptions;
using Common.Di.SharedInterfaces;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Common.Di;

/*
*   БЫЛО:
*   [Load → ConfigureSharedInterface → UseSharedInterface → OnSharedInterfaceInjected → OnAllPluginsLoaded → Unload]
*
*   СТАЛО:
*   [OnLoad → OnStart → OnReady → OnUnload → OnStop] 
*/
public abstract class Plugin<TModule>(ISwiftlyCore core) : BasePlugin(core) where TModule : IModule
{
    private bool _moduleAttached;
    private int _cleanupStarted;
    /// <summary>
    /// Текущий модуль плагина.
    /// Гарантированно доступен только после стадии инициализации модуля.
    /// </summary>
    /// <exception cref="NotAttachedModuleException">
    /// Выбрасывается, если модуль ещё не был создан и привязан к плагину.
    /// </exception>
    protected TModule Module
    {
        get => field ?? throw new NotAttachedModuleException($"Module '{typeof(TModule).Name}' is not attached to plugin '{GetType().Name}'");
        set;
    }
    
    /// <summary>
    /// Получает обязательную зависимость из DI-контейнера модуля.
    /// </summary>
    /// <typeparam name="TService">Тип требуемого сервиса</typeparam>
    /// <returns>Экземпляр сервиса</returns>
    /// <exception cref="InvalidOperationException">
    /// Если сервис не зарегистрирован в контейнере
    /// </exception>
    protected static TService GetRequiredService<TService>() where TService : notnull
    {
        return DependencyResolver.GetRequiredService<TModule, TService>();
    }

    /// <summary>
    /// Получает зависимость из DI-контейнера с ленивой инициализацией.
    /// </summary>
    /// <typeparam name="TService">Тип сервиса</typeparam>
    /// <returns>Lazy-обертка для отложенного получения сервиса</returns>
    protected static Lazy<TService> GetRequiredServiceLazy<TService>() where TService : notnull
    {
        return DependencyResolver.GetRequiredServiceLazy<TModule, TService>();
    }
    
    /// <summary>
    /// Получает обязательный Shared Interface из SwiftlyS2
    /// и сохраняет его в зарегистрированной отложенной ссылке.
    /// </summary>
    protected static void BindSharedInterface<TInterface>(IInterfaceManager interfaceManager, string key) where TInterface : class
    {
        var sharedInterface = interfaceManager.GetSharedInterface<TInterface>(key);

        GetRequiredService<SharedInterfaceReference<TInterface>>().Bind(sharedInterface);
    }
    
    /// <summary>
    /// Вызывается ДО создания модуля.
    /// На этом этапе зависимости ещё недоступны.
    /// Используется для ранней настройки плагина.
    /// </summary>
    protected virtual void OnLoad() { }
    
    /// <summary>
    /// Вызывается ПОСЛЕ создания модуля и загрузки всех зависимостей.
    /// Основная точка входа для инициализации логики плагина.
    /// </summary>
    protected virtual void OnStart() { }
    
    protected virtual void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
    }

    protected virtual void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
    }

    protected virtual void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
    }
    
    /// <summary>
    /// Вызывается после внедрения shared-интерфейсов через <see cref="IInterfaceManager"/>.
    /// Подходит для подписки на внешние API или события или поздней инициализации плагина, можно считать, что это
    /// самый безопасный метод, когда все зависимости уже известны и все плагины поделились своими API.
    /// </summary>
    protected virtual void OnReady() { }
    
    /// <summary>
    /// Вызывается ДО уничтожения модуля.
    /// Все зависимости всё ещё доступны.
    /// Используется для корректного освобождения ресурсов.
    /// </summary>
    protected virtual void OnUnload() { }
    
    /// <summary>
    /// Вызывается ПОСЛЕ уничтожения модуля.
    /// Зависимости больше недоступны.
    /// Финальная стадия очистки.
    /// </summary>
    protected virtual void OnStop() { }
    
    /// <summary>
    /// Метод жизненного цикла загрузки плагина.
    /// <para>
    /// ⚠️ Не рекомендуется вызывать напрямую. Вместо этого используйте <see cref="OnLoad"/>, <see cref="OnStart"/> или <see cref="OnReady"/>.
    /// </para>
    /// </summary>
    /// <param name="hotReload">
    /// Флаг горячей перезагрузки плагина.
    /// Возможные сценарии:
    /// 1. старт сервера - false
    /// 2. первичная загрузка - false
    /// 3. reload плагина - true
    /// 4. замена dll на лету - true
    /// </param>
    public sealed override void Load(bool hotReload)
    {
        try
        {
            OnLoad();
            Module = DependencyManager.BuildModule<TModule>(Core);
            _moduleAttached = true;
            OnStart();
        }
        catch (Exception loadException)
        {
            try
            {
                Cleanup();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException("Plugin load and rollback failed.", loadException, cleanupException);
            }

            throw;
        }
    }
    
    public sealed override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        ExecuteOrRollback(() => OnConfigureSharedInterfaces(interfaceManager));
    }
    
    public sealed override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        ExecuteOrRollback(() => OnUseSharedInterfaces(interfaceManager));
    }

    /// <summary>
    /// Вызывается после внедрения интерфейсов платформы.
    /// </summary>
    public sealed override void OnSharedInterfaceInjected(IInterfaceManager interfaceManager)
    {
        ExecuteOrRollback(() => { OnSharedInterfacesInjected(interfaceManager); OnReady(); });
    }

    /// <summary>
    /// Метод жизненного цикла выгрузки плагина.
    /// <para>
    /// ⚠️ Не рекомендуется вызывать напрямую. Вместо этого используйте <see cref="OnUnload"/> или <see cref="OnStop"/>.
    /// </para>
    /// </summary>
    public sealed override void Unload()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (Interlocked.Exchange(ref _cleanupStarted, 1) != 0) return;

        List<Exception> failures = [];
        try { OnUnload(); } catch (Exception exception) { failures.Add(exception); }

        if (_moduleAttached)
        {
            try { DependencyManager.DestroyModule<TModule>(); }
            catch (Exception exception) { failures.Add(exception); }
            _moduleAttached = false;
        }

        try { OnStop(); } catch (Exception exception) { failures.Add(exception); }

        if (failures.Count == 1) throw failures[0];
        if (failures.Count > 1) throw new AggregateException("Plugin cleanup failed.", failures);
    }

    private void ExecuteOrRollback(Action phase)
    {
        try { phase(); }
        catch (Exception phaseException)
        {
            try { Cleanup(); }
            catch (Exception cleanupException)
            {
                throw new AggregateException("Plugin lifecycle phase and rollback failed.", phaseException, cleanupException);
            }
            throw;
        }
    }
}
