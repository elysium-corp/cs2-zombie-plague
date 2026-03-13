namespace CS2ZombiePlague.Config.models;

public sealed class ModelsConfig
{
    // Модели людей
    public List<string> DefaultHumanModels = ["characters/models/ctm_sas/ctm_sas.vmdl"];
    // Включить озвучку радиокоманд у кастомных моделей
    public bool EnableRadioCommands { get; set; } = true;
    // Список включенных моделей
    public readonly List<IModelConfig> Models = [new LionelMessi()];
}

public sealed class LionelMessi : IModelConfig
{
    public string InternalName { get; set; } = "messi";
    public string ModelPath { get; set; } = "characters/models/nozb1/lio_messi_player_model/lio_messi_player_model_ct.vmdl";
    public bool RadioCommandIsEnabled { get; set; } = true;

    public Dictionary<string, string> RadioCommands { get; set; } = new()
    {
        {"thanks", "FrostNade.hit"},
    };
}