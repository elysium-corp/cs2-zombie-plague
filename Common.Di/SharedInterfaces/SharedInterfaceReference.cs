namespace Common.Di.SharedInterfaces;

internal sealed class SharedInterfaceReference<TInterface> where TInterface : class
{
    private TInterface? _value;

    internal TInterface Value =>
        Volatile.Read(ref _value)
        ?? throw new InvalidOperationException($"Shared interface '{typeof(TInterface).Name}' has not been injected!");

    internal void Bind(TInterface value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (Interlocked.CompareExchange(ref _value, value, null) is not null)
        {
            throw new InvalidOperationException($"Shared interface '{typeof(TInterface).Name}' is already initialized!");
        }
    }
}