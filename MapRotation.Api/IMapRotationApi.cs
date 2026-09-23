namespace MapRotation.Api;

/// <summary>Состояние единственного владельца ротации карт.</summary>
public enum RotationState { Loading, Playing, Voting, NextMapSelected, FinalRound, ChangingMap }

/// <summary>Источник следующей карты; Provisional допускает замену голосованием.</summary>
public enum NextMapSource { Provisional, ScheduledVote, Rtv, Admin }

/// <summary>Снимок состояния, безопасный для хранения потребителем.</summary>
public sealed record MapRotationStatus(RotationState State, string CurrentMap, DateTimeOffset StartedAt,
    DateTimeOffset Deadline, string? NextMap, NextMapSource Source, int RtvVotes, int RtvRequired,
    Guid? VoteId, DateTimeOffset? VoteEndsAt);

/// <summary>Чтение ротации без SQL и без права изменять состояние; вызовы на игровом потоке.</summary>
public interface IMapRotationApi
{
    /// <summary>Ключ shared-интерфейса.</summary>
    public const string SharedApiKey = "MapRotation.Api.IMapRotationApi";

    /// <summary>Возвращает новый неизменяемый снимок состояния.</summary>
    MapRotationStatus GetStatus();
}
