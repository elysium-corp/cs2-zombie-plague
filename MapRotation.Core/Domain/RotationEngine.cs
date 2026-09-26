using System.Collections.Immutable;
using MapRotation.Api;

namespace MapRotation.Core.Domain;

internal sealed record VoteState(Guid Id, NextMapSource Source, DateTimeOffset StartedAt, DateTimeOffset EndsAt,
    ImmutableArray<RotationMap> Options, ImmutableDictionary<ulong, long> Votes);
internal sealed record VoteArchive(VoteState Vote, DateTimeOffset FinishedAt, long? WinnerId)
{
    public int EligibleVoterCount { get; init; }
}
internal sealed record MapHistoryEntry(Guid Id, string MapName, string WorkshopId, long? MapId,
    DateTimeOffset StartedAt, DateTimeOffset EndedAt);
internal sealed record RotationCheckpoint(string CurrentMap, string WorkshopId, Guid MapSessionId,
    DateTimeOffset StartedAt, DateTimeOffset Deadline, RotationState State, long? NextMapId, NextMapSource Source,
    DateTimeOffset? FinalRoundAt, DateTimeOffset? ChangeAt, VoteState? Vote,
    ImmutableArray<ulong> Rtv, ImmutableDictionary<ulong, long> Nominations, ImmutableArray<long> History)
{
    public DateTimeOffset? PausedAt { get; init; }
    public TimeSpan PausedDuration { get; init; }
    public bool NativeMatchEnded { get; init; }
    public DateTimeOffset? NativeDeadline { get; init; }
}
internal enum RotationReply { Accepted, Duplicate, Delay, Disabled, TooFewPlayers, NotEligible, Locked, InvalidMap }

/// <summary>Чистая машина состояний. Все изменения выполняет один игровой поток.</summary>
internal sealed class RotationEngine(TimeProvider clock, IRotationRandom random) : IMapRotationApi
{
    private readonly MapCandidates _candidates = new(random);
    private readonly HashSet<ulong> _eligible = [];
    private readonly HashSet<ulong> _rtv = [];
    private readonly Dictionary<ulong, long> _nominations = [];
    private ImmutableHashSet<long> _valid = [];
    private ImmutableArray<long> _history = [];
    private DateTimeOffset? _finalRoundAt;
    private DateTimeOffset? _changeAt;
    private DateTimeOffset _retryChangeAt;
    private Guid _mapSessionId;
    private bool _mapUnloaded;
    private int _eligiblePlayerCount;
    private int _humanPlayerCount;
    private DateTimeOffset? _pausedAt;
    private TimeSpan _pausedDuration;
    private DateTimeOffset? _nativeDeadline;
    private bool _nativeMatchEnded;
    private DateTimeOffset EffectiveDeadline => _nativeDeadline is { } native && native < Deadline ? native : Deadline;
    public RotationConfiguration Configuration { get; private set; } = RotationConfiguration.Empty;
    public RotationState State { get; private set; } = RotationState.Loading;
    public string CurrentMap { get; private set; } = "";
    public string WorkshopId { get; private set; } = "";
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset Deadline { get; private set; }
    public long? NextMapId { get; private set; }
    public NextMapSource Source { get; private set; }
    public VoteState? Vote { get; private set; }
    public VoteArchive? LastResult { get; private set; }
    public long Revision { get; private set; }
    public RotationPauseReason PauseReason { get; private set; }
    public bool RotationEnabled => Configuration.Maps.Any(map => IsAllowed(map, _ => true));
    private DateTimeOffset TimerNow => _pausedAt ?? clock.GetUtcNow();
    public RotationSettings Settings => Configuration.Settings;
    public int RtvRequired => Math.Max(Settings.RtvMinVotes, (int)Math.Ceiling(_eligiblePlayerCount * Settings.RtvRatio));
    public int RtvVotes => _rtv.Count;
    public int EligibleVoterCount => _eligible.Count;
    public int RtvDelayRemaining => Math.Max(0, (int)Math.Ceiling((StartedAt.Add(_pausedDuration).AddSeconds(Settings.RtvDelaySeconds) - TimerNow).TotalSeconds));
    public RotationMap? NextMap => Configuration.Maps.FirstOrDefault(map => map.Id == NextMapId);
    public TimeSpan TimeLeft => EffectiveDeadline > TimerNow ? EffectiveDeadline - TimerNow : TimeSpan.Zero;
    public event Action<RotationMap, bool>? ChangeRequested;
    public event Action<VoteArchive>? VoteFinished;
    public event Action<MapHistoryEntry>? MapFinished;

    public MapRotationStatus GetStatus() => new(State, CurrentMap, StartedAt, ProjectTime(EffectiveDeadline), NextMap?.MapName,
        Source, _rtv.Count, RtvRequired, Vote?.Id, Vote is { } vote ? ProjectTime(vote.EndsAt) : null)
        { RotationEnabled = RotationEnabled, PauseReason = PauseReason,
            TimeLeftSeconds = PauseReason == RotationPauseReason.NoMaps ? null : (int)Math.Ceiling(TimeLeft.TotalSeconds) };

    public void Configure(RotationConfiguration configuration, IEnumerable<long> validIds)
    {
        Configuration = configuration;
        _valid = validIds.ToImmutableHashSet();
        foreach (var (steam, id) in _nominations.ToArray())
            if (!IsAllowed(id, map => map.AllowNomination && map.AllowVote)) _nominations.Remove(steam);
        if (State != RotationState.Loading && !IsAllowed(NextMapId, map => Source != NextMapSource.Provisional || AutoOrFallback(map)))
        {
            NextMapId = null;
            Source = NextMapSource.Provisional;
            if (State == RotationState.NextMapSelected) State = RotationState.Playing;
            SelectProvisional();
        }
        RecoverMissingTarget();
        UpdateClock();
        Revision++;
        TryRtvThreshold();
    }

    public void LoadMap(string name, string workshop, RotationCheckpoint? saved = null)
    {
        CurrentMap = name; WorkshopId = workshop;
        _eligible.Clear(); _rtv.Clear(); _nominations.Clear(); Vote = null; LastResult = null;
        _eligiblePlayerCount = 0; _humanPlayerCount = 0; _mapUnloaded = false;
        _changeAt = null; _finalRoundAt = null; _retryChangeAt = default;
        _pausedAt = clock.GetUtcNow(); _pausedDuration = TimeSpan.Zero;
        _nativeDeadline = null; _nativeMatchEnded = false;
        // ChangingMap — это уже завершённая сессия, даже при повторной загрузке той же карты.
        if (saved is not null && saved.CurrentMap == name && saved.WorkshopId == workshop
            && saved.State is not (RotationState.Loading or RotationState.ChangingMap))
        {
            _mapSessionId = saved.MapSessionId; StartedAt = saved.StartedAt; Deadline = saved.Deadline;
            State = saved.State; NextMapId = saved.NextMapId; Source = saved.Source;
            Vote = saved.Vote; _finalRoundAt = saved.FinalRoundAt; _changeAt = saved.ChangeAt;
            if (Vote is { Options.Length: > 6 } restored)
            {
                var options = restored.Options.Take(6).ToImmutableArray();
                var ids = options.Select(map => map.Id).ToHashSet();
                Vote = restored with { Options = options, Votes = restored.Votes.Where(pair => ids.Contains(pair.Value)).ToImmutableDictionary() };
            }
            _pausedAt = saved.PausedAt ?? clock.GetUtcNow(); _pausedDuration = saved.PausedDuration;
            _nativeMatchEnded = saved.NativeMatchEnded;
            _nativeDeadline = saved.NativeDeadline;
            _rtv.UnionWith(saved.Rtv);
            foreach (var pair in saved.Nominations) _nominations[pair.Key] = pair.Value;
            _history = saved.History;
        }
        else
        {
            _mapSessionId = Guid.NewGuid(); StartedAt = clock.GetUtcNow();
            Deadline = StartedAt.AddSeconds(Settings.MapDurationSeconds);
            State = RotationState.Playing; NextMapId = null; Source = NextMapSource.Provisional;
            if (saved is not null) _history = saved.History;
        }
        RecoverMissingTarget();
        if (!IsAllowed(NextMapId, map => Source != NextMapSource.Provisional || AutoOrFallback(map))) SelectProvisional();
        RecoverMissingTarget();
        UpdateClock();
        Revision++;
    }

    public void SetPlayers(IEnumerable<ulong> players, int? eligiblePlayerCount = null, int? humanPlayerCount = null)
    {
        var next = players.Where(id => id != 0).ToHashSet();
        var count = Math.Max(next.Count, eligiblePlayerCount ?? next.Count);
        _humanPlayerCount = Math.Max(0, humanPlayerCount ?? next.Count);
        UpdateClock();
        if (_eligiblePlayerCount == count && _eligible.SetEquals(next) && _rtv.All(next.Contains) && _nominations.Keys.All(next.Contains)
            && (Vote is null || Vote.Votes.Keys.All(next.Contains))) return;
        _eligible.Clear(); _eligible.UnionWith(next);
        _eligiblePlayerCount = count;
        _rtv.RemoveWhere(id => !next.Contains(id));
        foreach (var steam in _nominations.Keys.Where(id => !next.Contains(id)).ToArray()) _nominations.Remove(steam);
        if (Vote is { } vote) Vote = vote with { Votes = vote.Votes.RemoveRange(vote.Votes.Keys.Where(id => !next.Contains(id))) };
        Revision++;
        TryRtvThreshold();
    }

    public RotationReply Rtv(ulong steam)
    {
        if (!RotationEnabled || !Settings.RtvEnabled) return RotationReply.Disabled;
        if (!_eligible.Contains(steam)) return RotationReply.NotEligible;
        if (State != RotationState.Playing || Source != NextMapSource.Provisional) return RotationReply.Locked;
        if (RtvDelayRemaining > 0) return RotationReply.Delay;
        if (_eligiblePlayerCount < Settings.RtvMinPlayers) return RotationReply.TooFewPlayers;
        if (!_rtv.Add(steam)) return RotationReply.Duplicate;
        Revision++;
        TryRtvThreshold();
        return RotationReply.Accepted;
    }

    private void TryRtvThreshold()
    {
        if (Settings.RtvEnabled && RtvDelayRemaining == 0 && _eligiblePlayerCount >= Settings.RtvMinPlayers && _rtv.Count >= RtvRequired)
            StartVote(NextMapSource.Rtv);
    }

    public ImmutableArray<RotationMap> NominationMaps() => Eligible(map => map.AllowNomination && map.AllowVote, Settings.VoteOptionsCount);
    public ImmutableArray<CatalogMap> Catalog()
    {
        var nominations = NominationMaps().Select(map => map.Id).ToHashSet();
        return Configuration.Maps.Select(map =>
        {
            var current = map.IsCurrent(CurrentMap, WorkshopId);
            var exclusion = !map.Enabled ? "Disabled"
                : !_valid.Contains(map.Id) ? "EngineRejected"
                : current && !Settings.AllowSameMap ? "CurrentMap" : null;
            return new CatalogMap(map.Id, map.Key, map.MapName, map.WorkshopId, map.Enabled,
                _valid.Contains(map.Id), current, map.AllowNomination, map.AllowVote, map.AllowAutoRotation,
                exclusion ?? (!map.AllowVote && !AutoOrFallback(map) ? "NoVotingOrRotation" : null),
                exclusion ?? (!Settings.NominationsEnabled ? "NominationsDisabled"
                    : !map.AllowNomination ? "NominationDisabled" : !map.AllowVote ? "VotingDisabled"
                    : !nominations.Contains(map.Id) ? "RecentMap" : null));
        }).ToImmutableArray();
    }
    public long? Nomination(ulong steam) => _nominations.TryGetValue(steam, out var id) ? id : null;
    public RotationReply Nominate(ulong steam, long mapId)
    {
        if (!RotationEnabled || !Settings.NominationsEnabled) return RotationReply.Disabled;
        if (!_eligible.Contains(steam)) return RotationReply.NotEligible;
        if (State != RotationState.Playing || Source != NextMapSource.Provisional) return RotationReply.Locked;
        if (!NominationMaps().Any(map => map.Id == mapId)) return RotationReply.InvalidMap;
        _nominations[steam] = mapId; Revision++;
        return RotationReply.Accepted;
    }

    public bool StartVote(NextMapSource source)
    {
        if (!RotationEnabled || State != RotationState.Playing || Source != NextMapSource.Provisional || Vote is not null
            || _humanPlayerCount == 0) return false;
        var options = _candidates.VoteOptions(Eligible(map => map.AllowVote, Settings.VoteOptionsCount), _nominations, Settings);
        if (options.IsEmpty) return false;
        var now = TimerNow;
        Vote = new(Guid.NewGuid(), source, now, now.AddSeconds(Settings.VoteDurationSeconds), options, ImmutableDictionary<ulong, long>.Empty);
        State = RotationState.Voting; UpdateClock(); Revision++;
        return true;
    }

    /// <param name="allowChange">Можно ли заменить уже поданный голос; иначе учитывается только первый выбор.</param>
    public bool CastVote(ulong steam, Guid voteId, long mapId, bool allowChange = true)
    {
        if (Vote is not { } vote || vote.Id != voteId || clock.GetUtcNow() >= vote.EndsAt || !_eligible.Contains(steam)
            || !vote.Options.Any(map => map.Id == mapId)) return false;
        if (!allowChange && vote.Votes.ContainsKey(steam)) return false;
        Vote = vote with { Votes = vote.Votes.SetItem(steam, mapId) }; Revision++;
        return true;
    }

    public void Tick()
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || PauseReason != RotationPauseReason.None) return;
        var now = clock.GetUtcNow();
        if (now >= Deadline && State != RotationState.FinalRound) EnterFinalRound();
        if (Vote is { } vote && now >= vote.EndsAt) CompleteVote();
        if (_nativeMatchEnded) { ChangeAfterNativeMatch(); return; }
        if (State == RotationState.Playing && Settings.ScheduledVoteEnabled
            && now >= Deadline.AddSeconds(-Settings.ScheduledVoteBeforeSeconds)) StartVote(NextMapSource.ScheduledVote);
        if (State == RotationState.FinalRound && now >= (_finalRoundAt ?? Deadline).AddSeconds(Settings.FinalRoundTimeoutSeconds))
            RequestChange(forced: true);
        else if (_changeAt is { } changeAt && now >= changeAt) RequestChange(forced: false);
    }

    public void RoundEnded()
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || PauseReason != RotationPauseReason.None) return;
        if (_nativeMatchEnded) { ChangeAfterNativeMatch(); return; }
        if (clock.GetUtcNow() >= Deadline && State != RotationState.FinalRound) EnterFinalRound();
        if (State != RotationState.FinalRound) return;
        if (Vote is not null) CompleteVote();
        if (State != RotationState.FinalRound || PauseReason != RotationPauseReason.None) return;
        _changeAt ??= clock.GetUtcNow().AddSeconds(Source == NextMapSource.Rtv ? Settings.RtvChangeDelaySeconds : 0);
        if (clock.GetUtcNow() >= _changeAt) RequestChange(false);
        Revision++;
    }

    public void ObserveNativeMatch(NativeMatchProgress progress)
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || _mapUnloaded) return;
        if (progress.Ended) { MatchEnded(); return; }
        if (_nativeMatchEnded)
        {
            // Штатный перезапуск матча на той же карте отменяет отложенный переход
            // по старому событию завершения, но не обнуляет собственные часы ротации.
            _nativeMatchEnded = false; _changeAt = null;
            if (State == RotationState.FinalRound && TimerNow < Deadline && Source != NextMapSource.Rtv)
            {
                _finalRoundAt = null;
                State = Source == NextMapSource.Provisional ? RotationState.Playing : RotationState.NextMapSelected;
            }
            Revision++;
        }
        if (!RotationEnabled || progress.Warmup || !progress.Started) { _nativeDeadline = null; return; }
        if (PauseReason != RotationPauseReason.None) return;
        _nativeDeadline = progress.SecondsRemaining is { } seconds && double.IsFinite(seconds) && seconds is >= 0 and <= 604800
            ? TimerNow.AddSeconds(seconds) : null;
        if (Settings.ScheduledVoteEnabled && (progress.RoundsRemaining is >= 0 and <= 2
            || _nativeDeadline <= TimerNow.AddSeconds(Settings.ScheduledVoteBeforeSeconds)))
            StartVote(NextMapSource.ScheduledVote);
    }

    public void MatchEnded()
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || _mapUnloaded) return;
        if (RotationEnabled && PauseReason != RotationPauseReason.NoMaps) _nativeDeadline = TimerNow;
        if (_nativeMatchEnded) return;
        _nativeMatchEnded = true; Revision++;
        if (PauseReason == RotationPauseReason.None && Settings.ScheduledVoteEnabled)
            StartVote(NextMapSource.ScheduledVote);
        ChangeAfterNativeMatch();
    }

    private void ChangeAfterNativeMatch()
    {
        if (PauseReason != RotationPauseReason.None || !RotationEnabled || Vote is not null) return;
        if (Settings.ScheduledVoteEnabled && StartVote(NextMapSource.ScheduledVote)) return;
        if (NextMap is null) return;
        // Даём обработчикам конца матча завершиться. Голосование, если оно ещё
        // идёт, сохраняет свой срок; событие конца матча запоминается до результата.
        _changeAt ??= clock.GetUtcNow().AddSeconds(Math.Max(3, Source == NextMapSource.Rtv ? Settings.RtvChangeDelaySeconds : 0));
        if (clock.GetUtcNow() >= _changeAt) RequestChange(false);
        else if (State != RotationState.FinalRound) { EnterFinalRound(); }
    }

    public bool SetNext(long id, bool changeNow)
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || !IsAllowed(id, _ => true)) return false;
        CancelVote();
        NextMapId = id; Source = NextMapSource.Admin;
        if (State != RotationState.FinalRound) State = RotationState.NextMapSelected;
        UpdateClock();
        Revision++;
        if (changeNow) RequestChange(false);
        return true;
    }

    private void CompleteVote()
    {
        if (Vote is not { } vote) return;
        var permitted = vote.Options.Where(map => IsAllowed(map.Id, current => current.AllowVote)).ToArray();
        var counts = permitted.Select(map => (Map: map, Count: vote.Votes.Values.Count(id => id == map.Id))).ToArray();
        var max = counts.Length == 0 ? 0 : counts.Max(entry => entry.Count);
        RotationMap? winner = max > 0 ? _candidates.Tie(counts.Where(entry => entry.Count == max).ToArray()).Map
            : IsAllowed(NextMapId, AutoOrFallback) ? NextMap
            : _candidates.Weighted(permitted) ?? _candidates.Weighted(Eligible(map => map.AllowAutoRotation, 1));
        Vote = null;
        NextMapId = winner?.Id; Source = vote.Source;
        var result = new VoteArchive(vote, clock.GetUtcNow(), winner?.Id) { EligibleVoterCount = _eligible.Count };
        LastResult = result;
        if (State != RotationState.FinalRound) State = RotationState.NextMapSelected;
        if (winner is null)
        {
            State = RotationState.Playing; Source = NextMapSource.Provisional;
            _finalRoundAt = null; _changeAt = null;
            Deadline = TimerNow.AddSeconds(Settings.MapDurationSeconds);
        }
        else if (vote.Source == NextMapSource.Rtv)
        {
            EnterFinalRound();
            if (Settings.RtvChangeMode == "immediate") _changeAt = clock.GetUtcNow().AddSeconds(Settings.RtvChangeDelaySeconds);
        }
        UpdateClock();
        Revision++;
        VoteFinished?.Invoke(result);
    }

    private void CancelVote()
    {
        if (Vote is not { } vote) return;
        Vote = null;
        VoteFinished?.Invoke(new(vote, clock.GetUtcNow(), null) { EligibleVoterCount = _eligible.Count });
    }

    private void EnterFinalRound()
    {
        if (NextMap is null && Vote is null) return;
        State = RotationState.FinalRound;
        _finalRoundAt ??= NextMap is not null && clock.GetUtcNow() >= Deadline ? Deadline : clock.GetUtcNow();
        Revision++;
    }

    private void RequestChange(bool forced)
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || clock.GetUtcNow() < _retryChangeAt) return;
        if (Vote is not null) CompleteVote();
        if (!IsAllowed(NextMapId, _ => true)) SelectProvisional();
        if (NextMap is not { } map) { RecoverMissingTarget(); UpdateClock(); return; }
        State = RotationState.ChangingMap; Revision++;
        ChangeRequested?.Invoke(map, forced);
    }

    public void ChangeFailed(long mapId)
    {
        _valid = _valid.Remove(mapId);
        State = RotationState.FinalRound; _finalRoundAt ??= clock.GetUtcNow();
        _retryChangeAt = clock.GetUtcNow().AddSeconds(15);
        _changeAt = _retryChangeAt;
        SelectProvisional(); RecoverMissingTarget(); UpdateClock(); Revision++;
    }

    private DateTimeOffset ProjectTime(DateTimeOffset value) =>
        _pausedAt is { } pausedAt ? value.Add(clock.GetUtcNow() - pausedAt) : value;

    private void UpdateClock()
    {
        if (State == RotationState.Loading) return;
        if (!RotationEnabled) DisableEmptyPool();
        if (State == RotationState.ChangingMap) return;
        // Право голосовать и наличие человека различаются: настройки ботов/наблюдателей
        // не должны заставлять пустой сервер расходовать время карты.
        var hasTarget = IsAllowed(NextMapId, _ => true)
            || Vote is not null
            || Configuration.Maps.Any(map => IsAllowed(map, current => current.AllowVote));
        var reason = !hasTarget ? RotationPauseReason.NoMaps
            : _humanPlayerCount == 0 ? RotationPauseReason.EmptyServer : RotationPauseReason.None;
        if (reason != RotationPauseReason.None)
        {
            _pausedAt ??= clock.GetUtcNow();
        }
        else if (_pausedAt is { } pausedAt)
        {
            var duration = clock.GetUtcNow() - pausedAt;
            _pausedDuration += duration;
            Deadline = Deadline.Add(duration);
            if (_nativeDeadline is { } native) _nativeDeadline = native.Add(duration);
            _finalRoundAt = _finalRoundAt?.Add(duration);
            _changeAt = _changeAt?.Add(duration);
            if (_retryChangeAt != default) _retryChangeAt = _retryChangeAt.Add(duration);
            if (Vote is { } active) Vote = active with { StartedAt = active.StartedAt.Add(duration), EndsAt = active.EndsAt.Add(duration) };
            _pausedAt = null;
        }
        if (PauseReason != reason) { PauseReason = reason; Revision++; }
    }

    private void DisableEmptyPool()
    {
        // Удаление последней карты отменяет и восстановленное голосование, и отложенную смену.
        var changed = State != RotationState.Playing || Vote is not null || NextMapId is not null
            || _rtv.Count > 0 || _nominations.Count > 0 || LastResult is not null;
        CancelVote();
        NextMapId = null;
        RecoverMissingTarget();
        State = RotationState.Playing; Source = NextMapSource.Provisional; LastResult = null;
        _finalRoundAt = null; _changeAt = null; _retryChangeAt = default;
        _nativeDeadline = null;
        _rtv.Clear(); _nominations.Clear();
        if (Deadline <= TimerNow) Deadline = TimerNow.AddSeconds(Settings.MapDurationSeconds);
        if (changed) Revision++;
    }

    private void RecoverMissingTarget()
    {
        if (State != RotationState.FinalRound || NextMap is not null || Vote is not null) return;
        // Восстанавливаем в том числе старый checkpoint, зависший без следующей карты.
        State = RotationState.Playing; Source = NextMapSource.Provisional;
        _finalRoundAt = null; _changeAt = null; _retryChangeAt = default;
        Deadline = TimerNow.AddSeconds(Settings.MapDurationSeconds);
        Revision++;
    }

    public void UnloadMap()
    {
        if (CurrentMap.Length == 0 || State == RotationState.Loading || _mapUnloaded) return;
        _mapUnloaded = true;
        CancelVote();
        var id = Configuration.Maps.FirstOrDefault(map => map.IsCurrent(CurrentMap, WorkshopId))?.Id;
        if (id.HasValue) _history = _history.Insert(0, id.Value).Take(1000).ToImmutableArray();
        MapFinished?.Invoke(new(_mapSessionId, CurrentMap, WorkshopId, id, StartedAt, clock.GetUtcNow()));
        State = RotationState.ChangingMap;
        Revision++;
    }

    private bool IsAllowed(long? id, Func<RotationMap, bool> purpose) => id.HasValue
        && Configuration.Maps.Any(map => map.Id == id && IsAllowed(map, purpose));
    private bool IsAllowed(RotationMap map, Func<RotationMap, bool> purpose) =>
        map.Enabled && _valid.Contains(map.Id) && purpose(map)
        && (Settings.AllowSameMap || !map.IsCurrent(CurrentMap, WorkshopId));
    private ImmutableArray<RotationMap> Eligible(Func<RotationMap, bool> purpose, int count)
        => _candidates.Eligible(Configuration, CurrentMap, WorkshopId, _history, _valid, purpose, count);
    private void SelectProvisional()
    {
        NextMapId = _candidates.Weighted(Eligible(map => map.AllowAutoRotation, 1))?.Id;
        // Явно назначенный fallback страхует пустой auto-пул, но не обходит безопасность карты.
        if (NextMapId is null && IsAllowed(Settings.FallbackMapId, _ => true)) NextMapId = Settings.FallbackMapId;
        Source = NextMapSource.Provisional;
    }
    private bool AutoOrFallback(RotationMap map) => map.AllowAutoRotation || map.Id == Settings.FallbackMapId;

    public RotationCheckpoint Checkpoint() => new(CurrentMap, WorkshopId, _mapSessionId, StartedAt, Deadline,
        State, NextMapId, Source, _finalRoundAt, _changeAt, Vote, _rtv.ToImmutableArray(), _nominations.ToImmutableDictionary(), _history)
        { PausedAt = _pausedAt, PausedDuration = _pausedDuration, NativeMatchEnded = _nativeMatchEnded, NativeDeadline = _nativeDeadline };
}
