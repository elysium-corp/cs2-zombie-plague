namespace ZPCore.Config.SupplyBox;

public interface ISupplyBoxConfig
{
    public bool IsEnabled { get; set;}
    public int RespawnTimeBySeconds { get; set;}
    public int TimeSpreadBySeconds {get; set;}
    public int MaxCountTogether { get; set;}
    public int ChanceDrop { get; set;}
}