namespace ZombiePlague.Api.Data;

public interface IRound
{
    string Name { get; }

    void Start();

    void End();

    bool CanStart();
}