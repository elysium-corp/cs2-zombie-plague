namespace ZPApi.Data;

public interface IRound
{
    public int Chance { get; }
    
    public string Name { get; }
    
    public void Start();
    
    public void End();
}