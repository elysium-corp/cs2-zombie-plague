namespace ZPCore.Data.Weapons.Knifes;

internal interface IKnifeFactory
{
    public IKnife Create<T>() where T : IKnife;
    
}