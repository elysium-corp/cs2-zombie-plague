using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Events.Contexts.Player;

/// <summary>Контекст попытки вернуть зомби в человеческую роль.</summary>
public struct PlayerDisinfectingContext(IPlayer player) : IPreHookContext
{
    /// <summary>Игрок, которого требуется вылечить.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно вылеченного игрока.</summary>
public readonly struct PlayerDisinfectedContext(IPlayer player) : IPostHookContext
{
    /// <summary>Вылеченный игрок.</summary>
    public IPlayer Player { get; } = player;
}

/// <summary>Контекст назначения обычной человеческой роли.</summary>
public struct PlayerHumanizingContext(IPlayer player) : IPreHookContext
{
    /// <summary>Игрок, которому требуется назначить человеческую роль.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно назначенной обычной человеческой роли.</summary>
public readonly struct PlayerHumanizedContext(IPlayer player) : IPostHookContext
{
    /// <summary>Игрок с назначенной человеческой ролью.</summary>
    public IPlayer Player { get; } = player;
}

/// <summary>Контекст попытки назначить игроку роль немезиса.</summary>
public struct PlayerBecomingNemesisContext(IPlayer player) : IPreHookContext
{
    /// <summary>Игрок, которому требуется назначить роль немезиса.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно назначенной роли немезиса.</summary>
public readonly struct PlayerBecameNemesisContext(IPlayer player) : IPostHookContext
{
    /// <summary>Игрок, ставший немезисом.</summary>
    public IPlayer Player { get; } = player;
}

/// <summary>Контекст попытки назначить игроку роль выжившего.</summary>
public struct PlayerBecomingSurvivorContext(IPlayer player) : IPreHookContext
{
    /// <summary>Игрок, которому требуется назначить роль выжившего.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно назначенной роли выжившего.</summary>
public readonly struct PlayerBecameSurvivorContext(IPlayer player) : IPostHookContext
{
    /// <summary>Игрок, ставший выжившим.</summary>
    public IPlayer Player { get; } = player;
}

/// <summary>Контекст попытки возродить игрока с уже назначенной ролью.</summary>
public struct PlayerRespawningContext(IPlayer player) : IPreHookContext
{
    /// <summary>Возрождаемый игрок.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно возрождённого игрока.</summary>
public readonly struct PlayerRespawnedContext(IPlayer player) : IPostHookContext
{
    /// <summary>Возрождённый игрок.</summary>
    public IPlayer Player { get; } = player;
}

/// <summary>Контекст применения ранее назначенной роли к живому игроку.</summary>
public struct PlayerApplyingRoleContext(IPlayer player) : IPreHookContext
{
    /// <summary>Игрок, к которому применяется роль.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно применённой роли игрока.</summary>
public readonly struct PlayerRoleAppliedContext(IPlayer player) : IPostHookContext
{
    /// <summary>Игрок, к которому применена роль.</summary>
    public IPlayer Player { get; } = player;
}

/// <summary>Контекст отключения эффектов текущей роли игрока.</summary>
public struct PlayerDeactivatingRoleContext(IPlayer player) : IPreHookContext
{
    /// <summary>Игрок, роль которого отключается.</summary>
    public IPlayer Player { get; set; } = player;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно отключённой роли игрока.</summary>
public readonly struct PlayerRoleDeactivatedContext(IPlayer player) : IPostHookContext
{
    /// <summary>Игрок, роль которого отключена.</summary>
    public IPlayer Player { get; } = player;
}
