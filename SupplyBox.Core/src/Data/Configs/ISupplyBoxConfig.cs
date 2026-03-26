namespace SupplyBox.Data.Configs;

public interface ISupplyBoxConfig
{
    public int RespawnTimeBySeconds { get; set;}
    
    public int TimeSpreadBySeconds {get; set;}
    
    public int MaxCountTogether { get; set;}
    
    public int ChanceDrop { get; set;}
    
    public string SupplyBoxModel { get; set; }
    
    public string ParachuteModel { get; set; }
    
    public string ParachuteSound { get; set; }
}