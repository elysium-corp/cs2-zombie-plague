using System.Collections.Immutable;
using MapRotation.Api;

namespace MapRotation.Core.Domain;

internal sealed record VoteState(Guid Id, NextMapSource Source, DateTimeOffset StartedAt, DateTimeOffset EndsAt,
    ImmutableArray<RotationMap> Options, ImmutableDictionary<ulong, long> Votes);
internal sealed record VoteArchive(VoteState Vote, DateTimeOffset FinishedAt, long? WinnerId);
internal sealed record MapHistoryEntry(Guid Id, string MapName, string WorkshopId, long? MapId,
    DateTimeOffset StartedAt, DateTimeOffset EndedAt);
internal sealed record RotationCheckpoint(string CurrentMap, string WorkshopId, Guid MapSessionId,
    DateTimeOffset StartedAt, DateTimeOffset Deadline, RotationState State, long? NextMapId, NextMapSource Source,
    DateTimeOffset? FinalRoundAt, DateTimeOffset? ChangeAt, VoteState? Vote,
    ImmutableArray<ulong> Rtv, ImmutableDictionary<ulong, long> Nominations, ImmutableArray<long> History);
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
    public RotationSettings Settings => Configuration.Settings;
    public int RtvRequired => Math.Max(Settings.RtvMinVotes, (int)Math.Ceiling(_eligiblePlayerCount * Settings.RtvRatio));
    public int RtvVotes => _rtv.Count;
    public int RtvDelayRemaining => Math.Max(0, (int)Math.Ceiling((StartedAt.AddSeconds(Settings.RtvDelaySeconds) - clock.GetUtcNow()).TotalSeconds));
    public RotationMap? NextMap => Configuration.Maps.FirstOrDefault(map => map.Id == NextMapId);
    public TimeSpan TimeLeft => Deadline > clock.GetUtcNow() ? Deadline - clock.GetUtcNow() : TimeSpan.Zero;
    public event Action<RotationMap, bool>? ChangeRequested;
    public event Action<VoteArchive>? VoteFinished;
    public event Action<MapHistoryEntry>? MapFinished;

    public MapRotationStatus GetStatus() => new(State, CurrentMap, StartedAt, Deadline, NextMap?.MapName,
        Source, _rtv.Count, RtvRequired, Vote?.Id, Vote?.EndsAt);

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
        Revision++;
        TryRtvThreshold();
    }

    public void LoadMap(string name, string workshop, RotationCheckpoint? saved = null)
    {
        CurrentMap = name; WorkshopId = workshop;
        _eligible.Clear(); _rtv.Clear(); _nominations.Clear(); Vote = null; LastResult = null;
        _eligiblePlayerCount = 0; _mapUnloaded = false;
        _changeAt = null; _finalRoundAt = null; _retryChangeAt = default;
        // ChangingMap — это уже завершённая сессия, даже при повторной загрузке той же карты.
        if (saved is not null && saved.CurrentMap == name && saved.WorkshopId == workshop
            && saved.State is not (RotationState.Loading or RotationState.ChangingMap))
        {
            _mapSessionId = saved.MapSessionId; StartedAt = saved.StartedAt; Deadline = saved.Deadline;
            State = saved.State; NextMapId = saved.NextMapId; Source = saved.Source;
            Vote = saved.Vote; _finalRoundAt = saved.FinalRoundAt; _changeAt = saved.ChangeAt;
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
        if (!IsAllowed(NextMapId, map => Source != NextMapSource.Provisional || AutoOrFallback(map))) SelectProvisional();
        Revision++;
    }

    public void SetPlayers(IEnumerable<ulong> players, int? eligiblePlayerCount = null)
    {
        var next = players.Where(id => id != 0).ToHashSet();
        var count = Math.Max(next.Count, eligiblePlayerCount ?? next.Count);
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
        if (!Settings.RtvEnabled) return RotationReply.Disabled;
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
    public long? Nomination(ulong steam) => _nominations.TryGetValue(steam, out var id) ? id : null;
    public RotationReply Nominate(ulong steam, long mapId)
    {
        if (!Settings.NominationsEnabled) return RotationReply.Disabled;
        if (!_eligible.Contains(steam)) return RotationReply.NotEligible;
        if (State != RotationState.Playing || Source != NextMapSource.Provisional) return RotationReply.Locked;
        if (!NominationMaps().Any(map => map.Id == mapId)) return RotationReply.InvalidMap;
        _nominations[steam] = mapId; Revision++;
        return RotationReply.Accepted;
    }

    public bool StartVote(NextMapSource source)
    {
        if (State != RotationState.Playing || Source != NextMapSource.Provisional || Vote is not null) return false;
        var options = _candidates.VoteOptions(Eligible(map => map.AllowVote, Settings.VoteOptionsCount), _nominations, Settings);
        if (options.IsEmpty) return false;
        var now = clock.GetUtcNow();
        Vote = new(Guid.NewGuid(), source, now, now.AddSeconds(Settings.VoteDurationSeconds), options, ImmutableDictionary<ulong, long>.Empty);
        State = RotationState.Voting; Revision++;
        return true;
    }

    public bool CastVote(ulong steam, Guid voteId, long mapId)
    {
        if (Vote is not { } vote || vote.Id != voteId || clock.GetUtcNow() >= vote.EndsAt || !_eligible.Contains(steam)
            || !vote.Options.Any(map => map.Id == mapId)) return false;
        Vote = vote with { Votes = vote.Votes.SetItem(steam, mapId) }; Revision++;
        return true;
    }

    public void Tick()
    {
        if (State is RotationState.Loading or RotationState.ChangingMap) return;
        var now = clock.GetUtcNow();
        if (now >= Deadline && State != RotationState.FinalRound) EnterFinalRound();
        if (Vote is { } vote && now >= vote.EndsAt) CompleteVote();
        if (State == RotationState.Playing && Settings.ScheduledVoteEnabled
            && now >= Deadline.AddSeconds(-Settings.ScheduledVoteBeforeSeconds)) StartVote(NextMapSource.ScheduledVote);
        if (State == RotationState.FinalRound && now >= (_finalRoundAt ?? Deadline).AddSeconds(Settings.FinalRoundTimeoutSeconds))
            RequestChange(forced: true);
        else if (_changeAt is { } changeAt && now >= changeAt) RequestChange(forced: false);
    }

    public void RoundEnded()
    {
        if (State is RotationState.Loading or RotationState.ChangingMap) return;
        if (clock.GetUtcNow() >= Deadline && State != RotationState.FinalRound) EnterFinalRound();
        if (State != RotationState.FinalRound) return;
        if (Vote is not null) CompleteVote();
        _changeAt ??= clock.GetUtcNow().AddSeconds(Source == NextMapSource.Rtv ? Settings.RtvChangeDelaySeconds : 0);
        if (clock.GetUtcNow() >= _changeAt) RequestChange(false);
        Revision++;
    }

    public bool SetNext(long id, bool changeNow)
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || !IsAllowed(id, _ => true)) return false;
        CancelVote();
        NextMapId = id; Source = NextMapSource.Admin;
        if (State != RotationState.FinalRound) State = RotationState.NextMapSelected;
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
        LastResult = new(vote, clock.GetUtcNow(), winner?.Id);
        if (State != RotationState.FinalRound) State = RotationState.NextMapSelected;
        if (vote.Source == NextMapSource.Rtv)
        {
            EnterFinalRound();
            if (Settings.RtvChangeMode == "immediate") _changeAt = clock.GetUtcNow().AddSeconds(Settings.RtvChangeDelaySeconds);
        }
        Revision++;
        VoteFinished?.Invoke(LastResult);
    }

    private void CancelVote()
    {
        if (Vote is not { } vote) return;
        Vote = null;
        VoteFinished?.Invoke(new(vote, clock.GetUtcNow(), null));
    }

    private void EnterFinalRound()
    {
        State = RotationState.FinalRound;
        _finalRoundAt ??= clock.GetUtcNow() >= Deadline ? Deadline : clock.GetUtcNow();
        Revision++;
    }

    private void RequestChange(bool forced)
    {
        if (State is RotationState.Loading or RotationState.ChangingMap || clock.GetUtcNow() < _retryChangeAt) return;
        if (Vote is not null) CompleteVote();
        if (!IsAllowed(NextMapId, _ => true)) SelectProvisional();
        if (NextMap is not { } map) { _retryChangeAt = clock.GetUtcNow().AddSeconds(15); return; }
        State = RotationState.ChangingMap; Revision++;
        ChangeRequested?.Invoke(map, forced);
    }

    public void ChangeFailed(long mapId)
    {
        _valid = _valid.Remove(mapId);
        State = RotationState.FinalRound; _finalRoundAt ??= clock.GetUtcNow();
        _retryChangeAt = clock.GetUtcNow().AddSeconds(15);
        _changeAt = _retryChangeAt;
        SelectProvisional(); Revision++;
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
        && Configuration.Maps.Any(map => map.Id == id && map.Enabled && _valid.Contains(map.Id) && purpose(map)
            && (Settings.AllowSameMap || !map.IsCurrent(CurrentMap, WorkshopId)));
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
        State, NextMapId, Source, _finalRoundAt, _changeAt, Vote, _rtv.ToImmutableArray(), _nominations.ToImmutableDictionary(), _history);
}
