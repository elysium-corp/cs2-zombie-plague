using ZombiePlague.Api.Data;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class None : IRound
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