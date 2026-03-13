namespace CS2ZombiePlague.Config.Round;

public interface IRoundConfig
{
    public bool Enable { get; set; }
    
    public int Chance{ get; set; }
}