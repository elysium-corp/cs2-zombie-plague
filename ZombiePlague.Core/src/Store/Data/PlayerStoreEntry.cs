namespace ZombiePlague.Core.Store.Data;

internal sealed record PlayerStoreEntry(
    int PlayerId,
    PlayerPreferences Preferences
);