namespace Statistics.Core.Data;

internal sealed class RoundParticipantState
{
    public PlayerRole CurrentRole { get; set; }

    public bool WasHuman { get; set; }

    public bool WasZombie { get; set; }

    public bool WasFirstZombie { get; set; }

    public bool WasLastHuman { get; set; }
}

