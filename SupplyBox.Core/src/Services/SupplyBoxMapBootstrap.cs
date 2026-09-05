namespace SupplyBox.Services;

internal static class SupplyBoxMapBootstrap
{
    public static bool TryLoadCurrentMap(Func<string?> readMapName, Action<string> loadMap)
    {
        string? mapName;
        try
        {
            mapName = readMapName();
        }
        catch (InvalidOperationException)
        {
            // SwiftlyS2 выбрасывает InvalidOperationException, пока GlobalVars не созданы.
            // OnMapLoad передаст имя карты, когда движок будет готов.
            return false;
        }

        if (string.IsNullOrWhiteSpace(mapName)) return false;

        // Ошибки загрузки конфигурации не являются отсутствием карты и не подавляются.
        loadMap(mapName);
        return true;
    }
}
