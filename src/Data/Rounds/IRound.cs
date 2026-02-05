namespace CS2ZombiePlague.Data.Rounds;

public interface IRound
{
    public void Start();
    public void End();
    public int GetChance();
}