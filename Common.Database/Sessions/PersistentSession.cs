namespace Common.Database.Sessions;

public sealed class PersistentSession<TData>(TData data) where TData : class
{
    private readonly Lock _lock = new();

    private long _revision;
    private long _savedRevision;

    private bool _isLoaded;

    public SemaphoreSlim SaveLock { get; } = new(1, 1);

    public TResult Read<TResult>(Func<TData, TResult> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        lock (_lock)
        {
            return read(data);
        }
    }

    public void Update(Action<TData> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (_lock)
        {
            update(data);

            _revision++;
        }
    }

    public bool TryUpdate(Func<TData, bool> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (_lock)
        {
            if (!update(data))
            {
                return false;
            }

            _revision++;

            return true;
        }
    }

    public void CompleteLoad(Action<TData> applyLoadedData)
    {
        ArgumentNullException.ThrowIfNull(applyLoadedData);

        lock (_lock)
        {
            if (_isLoaded)
            {
                return;
            }

            /*
             * Если игрок уже успел что-то изменить,
             * пока БД отвечала, не затираем его
             * новое состояние старыми данными из БД.
             */
            if (_revision == 0)
            {
                applyLoadedData(data);
            }

            _isLoaded = true;

            /*
             * В БД запись существует.
             *
             * Если revision > 0, локальные изменения
             * останутся Dirty.
             */
            _savedRevision = 0;
        }
    }
    
    public void CompleteLoadMerged(Action<TData> mergeLoadedData)
    {
        ArgumentNullException.ThrowIfNull(mergeLoadedData);

        lock (_lock)
        {
            if (_isLoaded)
            {
                return;
            }

            mergeLoadedData(data);

            _isLoaded = true;
            _savedRevision = 0;
        }
    }

    public void CompleteLoadAsNew()
    {
        lock (_lock)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;

            /*
             * Записи в БД нет.
             * Поэтому текущие default-данные должны
             * попасть в БД при ближайшем сохранении.
             */
            _savedRevision = -1;
        }
    }

    public PersistentSessionSnapshot<TResult> CreateSnapshot<TResult>(Func<TData, TResult> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_lock)
        {
            return new PersistentSessionSnapshot<TResult>(
                Data: snapshot(data),
                Revision: _revision,
                IsLoaded: _isLoaded,
                IsDirty: _revision != _savedRevision
            );
        }
    }

    public void MarkSaved(long revision)
    {
        lock (_lock)
        {
            if (revision > _savedRevision)
            {
                _savedRevision = revision;
            }
        }
    }
}