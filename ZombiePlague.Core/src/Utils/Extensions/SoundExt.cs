using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace ZombiePlague.Core.Utils.Extensions;

public static class SoundExt
{
    /// <summary>
    /// Воспроизводит глобальный звук (например, музыку раунда) для всех игроков без привязки к источнику на карте.
    /// </summary>
    /// <param name="soundName">Имя звукового файла или события.</param>
    /// <param name="volume">Громкость воспроизведения (по умолчанию 0.5f).</param>
    public static uint PlayGlobal(string soundName, float volume = 0.5f)
    {
        using var sound = new SoundEvent(soundName);
        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = -1;
        sound.Volume = volume;
        return sound.Emit();
    }

    /// <summary>
    /// Воспроизводит локальный звук для конкретного игрока без привязки к позиции на карте (например, интерфейс или музыка раунда).
    /// </summary>
    /// <param name="recipient">Игрок, который услышит звук.</param>
    /// <param name="soundName">Имя звукового файла или события.</param>
    /// <param name="volume">Громкость воспроизведения от 0.0f до 1.0f (по умолчанию 0.5f).</param>
    /// <returns>Идентификатор (ID) запущенного звукового события.</returns>
    public static uint PlayLocal(IPlayer recipient, string soundName, float volume = 0.5f)
    {
        using var sound = new SoundEvent(soundName);
        sound.Recipients.AddRecipient(recipient.PlayerID);
        sound.SourceEntityIndex = -1;
        sound.Volume = volume;
        return sound.Emit();
    }

    /// <summary>
    /// Воспроизводит позиционный звук, привязанный к конкретному игроку (например, использование способностей).
    /// </summary>
    /// <param name="source">Игрок, от которого будет исходить звук.</param>
    /// <param name="soundName">Имя звукового файла или события.</param>
    /// <param name="volume">Громкость воспроизведения.</param>
    public static void PlayAt(IPlayer source, string soundName, float volume)
    {
        using var sound = new SoundEvent
        {
            Name = soundName,
            Volume = volume,
            SourceEntityIndex = (int)source.RequiredPlayerPawn.Index
        };
        sound.Recipients.AddAllPlayers();
        sound.Emit();
    }
    
    /// <summary>
    /// Воспроизводит позиционный звук, привязанный к конкретному игроку (например, использование способностей).
    /// </summary>
    /// <param name="sourceIndex">Индекс сущности, от которой будет исходить звук.</param>
    /// <param name="soundName">Имя звукового файла или события.</param>
    /// <param name="volume">Громкость воспроизведения.</param>
    public static void PlayAtEntity(int sourceIndex, string soundName, float volume)
    {
        using var sound = new SoundEvent
        {
            Name = soundName,
            Volume = volume,
            SourceEntityIndex = sourceIndex
        };
        sound.Recipients.AddAllPlayers();
        sound.Emit();
    }
}