using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace CustomEquipment.Utils;

public static class SoundExt
{
    /// <summary>
    /// Воспроизводит глобальный звук (например, музыку раунда) для всех игроков без привязки к источнику на карте.
    /// </summary>
    /// <param name="soundName">Имя звукового файла или события.</param>
    /// <param name="volume">Громкость воспроизведения (по умолчанию 0.5f).</param>
    public static void PlayGlobal(string soundName, float volume = 0.5f)
    {
        using var sound = new SoundEvent(soundName);
        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = -1;
        sound.Volume = volume;
        sound.Emit();
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
    /// Воспроизводит 3D-звук в заданной точке игрового мира для всех игроков.
    /// </summary>
    /// <param name="soundName">Идентификатор или имя звукового события.</param>
    /// <param name="position">Координаты в мире, где должен прозвучать аудиоэффект.</param>
    /// <param name="volume">Уровень громкости воспроизведения (от 0.0 до 1.0).</param>
    public static void PlayInPlace(CBaseEntity source, string soundName, Vector position, float volume)
    {
        using var sound = new SoundEvent
        {
            Name = soundName,
            Volume = volume,
            SourceEntityIndex = (int)source.Index
        };

        sound.SetFloat3("public.position", position.X, position.Y, position.Z);
        sound.Recipients.AddAllPlayers();
        sound.Emit();
    }
    
    /// <summary>
    /// Воспроизводит локальный позиционный звук для конкретного игрока (слышит только сам игрок).
    /// </summary>
    /// <param name="source">Игрок, для которого воспроизводится звук и от чьего персонажа он исходит.</param>
    /// <param name="soundName">Имя звукового ресурса или события.</param>
    /// <param name="volume">Уровень громкости (от 0.0 до 1.0).</param>
    public static void PlayLocalSound(IPlayer source, string soundName, float volume)
    {
        using var sound = new SoundEvent
        {
            Name = soundName,
            Volume = volume,
            SourceEntityIndex = (int)source.RequiredPlayerPawn.Index
        };
        sound.Recipients.AddRecipient(source.PlayerID);
        sound.Emit();
    }
}