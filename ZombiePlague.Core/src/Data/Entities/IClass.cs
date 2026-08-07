namespace ZombiePlague.Core.Data.Entities;

internal interface IClass
{
    public string InternalName { get; set; }
    
    public string DisplayName { get; set; }
    
    public string Description { get; set; }
}