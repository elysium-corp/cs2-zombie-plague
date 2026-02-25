namespace CS2ZombiePlague.Data.Rounds;

public class None : IRound
{
    public int Chance { get; } = 0;
    public string Name { get; } = "None";

    public void Start()
    {
        
    }

    public void End()
    {
        
    }
}