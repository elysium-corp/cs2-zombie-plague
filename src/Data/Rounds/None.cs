using CS2ZombiePlague.Data.Rounds.Contracts;

namespace CS2ZombiePlague.Data.Rounds;

public class None : IRound
{
    public int Chance => 0;
    public string Name => "None";

    public void Start()
    {
    }

    public void End()
    {
    }
}