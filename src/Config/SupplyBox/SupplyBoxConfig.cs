namespace CS2ZombiePlague.Config.SupplyBox;

public class SupplyBoxConfig : ISupplyBoxConfig
{
    // Включить дроп контейнеров.
    public bool IsEnabled { get; set; }  = true;
    // Время до выпадения контейнера.
    public int RespawnTimeBySeconds { get; set; } = 120;
    // Дополнительный разброс времени к основному. Итоговая формула: RespawnTimeBySeconds + Random(0, TimeSpreadBySeconds).
    public int TimeSpreadBySeconds { get; set; } = 30;
    // Максимальное число контейнеров на карте.
    public int MaxCountTogether { get; set; } = 2;
    // Вероятность спавна контейнера после истечения таймера.
    public int ChanceDrop { get; set; } = 50;
}