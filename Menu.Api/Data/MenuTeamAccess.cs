namespace Menu.Api.Data;

[Flags]
public enum MenuTeamAccess : byte
{
    None = 0,

    Spectator = 1 << 0,
    T = 1 << 1,
    CT = 1 << 2,

    Players = T | CT,
    All = Spectator | Players
}