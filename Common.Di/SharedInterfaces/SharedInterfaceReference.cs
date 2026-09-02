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

        Interlocked.Exchange(ref _value, value);
    }
}
