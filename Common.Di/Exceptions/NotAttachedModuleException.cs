namespace Common.Di.Exceptions;

internal sealed class NotAttachedModuleException : SystemException
{
    public NotAttachedModuleException(string? value) : base(value) { }
    
    public NotAttachedModuleException() { }
}