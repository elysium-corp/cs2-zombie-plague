namespace CS2ZombiePlague.Data.Rounds.Contracts;

public interface IRound
{
    public int Chance { get; }
    public string Name { get; }
    
    public void Start();
    public void End();
}