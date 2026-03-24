namespace MSApi.Exceptions;

public sealed class MoneyServicesNotFoundException : SystemException
{
    public MoneyServicesNotFoundException(string? value) : base(value) { }
    
    public MoneyServicesNotFoundException() { }
}