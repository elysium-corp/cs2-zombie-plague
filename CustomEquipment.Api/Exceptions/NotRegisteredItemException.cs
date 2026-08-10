namespace CustomEquipment.Api.Exceptions;

public sealed class NotRegisteredItemException : SystemException
{
    public NotRegisteredItemException(string? value) : base(value) { }
        
    public NotRegisteredItemException() { }
}