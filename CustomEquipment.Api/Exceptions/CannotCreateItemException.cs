namespace CustomEquipment.Api.Exceptions;

public sealed class CannotCreateItemException : InvalidOperationException
{
    public CannotCreateItemException(string? value) : base(value) { }
        
    public CannotCreateItemException() { }
}