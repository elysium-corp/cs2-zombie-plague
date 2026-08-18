namespace ZombiePlague.Api.Data.Rounds;

public interface IRound
{
    string Id { get; }
    
    string Name { get; }

    void Start();

    void End();

    bool CanStart();
}