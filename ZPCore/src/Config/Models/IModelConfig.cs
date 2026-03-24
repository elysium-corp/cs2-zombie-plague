namespace ZPCore.Config.models;

public interface IModelConfig
{
    public string InternalName {get; set;}
    public string ModelPath { get; set; }
    public bool RadioCommandIsEnabled { get; set; }
    public Dictionary<string, string> RadioCommands { get; set; }
}