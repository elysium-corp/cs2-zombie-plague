namespace Common.Di.Exceptions;

internal sealed class ModuleNotCreatedException : SystemException
{
    public ModuleNotCreatedException(string? value) : base(value) { }
    
    public ModuleNotCreatedException() { }
}