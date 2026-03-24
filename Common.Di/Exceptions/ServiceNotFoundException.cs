namespace Common.Di.Exceptions;

internal sealed class ServiceNotFoundException : SystemException
{
    public ServiceNotFoundException(string? value) : base(value) { }
    
    public ServiceNotFoundException() { }
}