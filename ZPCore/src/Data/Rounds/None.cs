using ZPApi.Data;

namespace ZPCore.Data.Rounds;

internal class None : IRound
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