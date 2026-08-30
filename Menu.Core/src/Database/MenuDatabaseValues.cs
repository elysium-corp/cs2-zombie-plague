namespace Menu.Core.Database;

/// <summary>
/// Значения cross-runtime контракта таблиц Menu.Core.
/// </summary>
internal static class MenuDatabaseValues
{
    internal const string ProviderStatusOnline = "online";
    internal const string ProviderStatusOffline = "offline";
    internal const string ProviderStatusIncompatible = "incompatible";
    internal const string ProviderStatusApiOutdated = "api_outdated";
    internal const string ProviderStatusError = "error";

    internal const string DefinitionStatusDraft = "draft";
    internal const string DefinitionStatusPublished = "published";
    internal const string DefinitionStatusArchived = "archived";

    internal const string ExportTypeMenu = "menu";
    internal const string ExportTypeAction = "action";

    internal const string CommandTypeChat = "chat";
    internal const string CommandTypeConsole = "console";

    internal const string SuppressionNone = "none";
    internal const string SuppressionOnMatch = "on_match";
    internal const string SuppressionOnSuccess = "on_success";

    internal const string LoadedSourceDatabase = "database";
    internal const string LoadedSourceLastKnownGood = "lkg";
    internal const string LoadedSourceFallback = "fallback";

    internal const string ValidationNotLoaded = "not_loaded";
    internal const string ValidationValid = "valid";
    internal const string ValidationInvalid = "invalid";
    internal const string ValidationDegraded = "degraded";
}
