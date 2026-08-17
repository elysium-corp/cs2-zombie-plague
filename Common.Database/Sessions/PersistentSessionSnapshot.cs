namespace Common.Database.Sessions;

public readonly record struct PersistentSessionSnapshot<TData>(
    TData Data,
    long Revision,
    bool IsLoaded,
    bool IsDirty
);