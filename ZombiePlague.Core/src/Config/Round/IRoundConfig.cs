namespace ZombiePlague.Core.Config.Round;

public interface IRoundConfig
{
    public bool Enable { get; set; }
    
    public string Name { get; set; }
    
    public int Weight { get; set; }
}