namespace CustomEquipment.Api.Exceptions;

public sealed class CannotCreateItemException : KeyNotFoundException
{
    public CannotCreateItemException(string? value) : base(value) { }
        
    public CannotCreateItemException() { }
}