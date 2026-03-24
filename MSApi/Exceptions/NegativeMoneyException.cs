namespace MSApi.Exceptions;

public sealed class NegativeMoneyException : SystemException
{
    public NegativeMoneyException(string? value) : base(value) { }
    
    public NegativeMoneyException() { }
}