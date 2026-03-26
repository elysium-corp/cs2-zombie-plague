namespace SupplyBox.Data.Configs;

public class SupplyBoxConfig : ISupplyBoxConfig
{
    // Время до выпадения контейнера.
    public int RespawnTimeBySeconds { get; set; } = 120;
    
    // Дополнительный разброс времени к основному. Итоговая формула: RespawnTimeBySeconds + Random(0, TimeSpreadBySeconds).
    public int TimeSpreadBySeconds { get; set; } = 30;
    
    // Максимальное число контейнеров на карте.
    public int MaxCountTogether { get; set; } = 2;
    
    // Вероятность спавна контейнера после истечения таймера.
    public int ChanceDrop { get; set; } = 50;
    
    // Модель контейнера.
    public string SupplyBoxModel { get; set; } = "models/props/crates/cs2_drop_crate_01.vmdl";
    
    // Модель парашюта.
    public string ParachuteModel { get; set; } = "characters/nozb1/parachute/parachute_carbon/parachute_open.vmdl";
    
    // Звук развивающегося на ветру парашюта.
    public string ParachuteSound { get; set; } = "ZombiePlagueSupplyBox.SupplyboxFly";
}