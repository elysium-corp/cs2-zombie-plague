namespace CustomEquipment.Exceptions;

internal sealed class NotRegisteredItemException : SystemException
{
    public NotRegisteredItemException(string? value) : base(value) { }
        
    public NotRegisteredItemException() { }
}