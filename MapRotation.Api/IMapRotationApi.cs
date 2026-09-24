namespace MapRotation.Api;

/// <summary>Состояние единственного владельца ротации карт.</summary>
public enum RotationState { Loading, Playing, Voting, NextMapSelected, FinalRound, ChangingMap }

/// <summary>Источник следующей карты; Provisional допускает замену голосованием.</summary>
public enum NextMapSource { Provisional, ScheduledVote, Rtv, Admin }

/// <summary>Причина приостановки часов ротации; на запуск игровых раундов не влияет.</summary>
public enum RotationPauseReason
{
    /// <summary>Время карты отсчитывается.</summary>
    None,
    /// <summary>На сервере нет подключённых людей.</summary>
    EmptyServer,
    /// <summary>Нет допустимой следующей карты или кандидатов для голосования.</summary>
    NoMaps
}

/// <summary>Снимок состояния, безопасный для хранения потребителем.</summary>
public sealed record MapRotationStatus(RotationState State, string CurrentMap, DateTimeOffset StartedAt,
    DateTimeOffset Deadline, string? NextMap, NextMapSource Source, int RtvVotes, int RtvRequired,
    Guid? VoteId, DateTimeOffset? VoteEndsAt)
{
    /// <summary>Ротация доступна только при наличии разрешённых установленных карт в пуле.</summary>
    public bool RotationEnabled { get; init; }

    /// <summary>Причина паузы; при сериализации статуса выводится имя, а не номер.</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<RotationPauseReason>))]
    public RotationPauseReason PauseReason { get; init; }

    /// <summary>Остаток времени в секундах; без цели или кандидатов — null (без ограничения).</summary>
    public int? TimeLeftSeconds { get; init; }
}

/// <summary>Чтение ротации без SQL и без права изменять состояние; вызовы на игровом потоке.</summary>
public interface IMapRotationApi
{
    /// <summary>Ключ shared-интерфейса.</summary>
    public const string SharedApiKey = "MapRotation.Api.IMapRotationApi";

    /// <summary>Возвращает новый неизменяемый снимок состояния.</summary>
    MapRotationStatus GetStatus();
}
