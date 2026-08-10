namespace CustomEquipment.Api.Exceptions;

public sealed class NotRegisteredItemException : KeyNotFoundException
{
    public NotRegisteredItemException(string? value) : base(value) { }
        
    public NotRegisteredItemException() { }
}