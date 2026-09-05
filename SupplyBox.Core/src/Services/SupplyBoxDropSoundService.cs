using Microsoft.Extensions.Logging;
using SupplyBox.Configuration;
using SupplyBox.Data.Configs;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace SupplyBox.Services;

internal sealed class SupplyBoxDropSoundService(ISwiftlyCore core)
{
    private readonly Queue<uint> _events = new();
    private const int MaximumTrackedEvents = 128;

    public void Play(SupplyBoxType type, SupplyBoxConfig settings)
    {
        var name = SupplyBoxSoundEvents.Choose(type.DropSoundEvents ?? settings.DropSoundEvents, Random.Shared.Next);
        if (name is null) return;
        try
        {
            // Сигнал слышен каждому получателю независимо от расстояния до ящика
            // Подбор ящика не обрывает сигнал, остановка выполняется при завершении раунда/карты
            using var sound = new SoundEvent { Name = name, SourceEntityIndex = -1, Volume = 1 };
            sound.Recipients.AddAllPlayers();
            var id = sound.Emit();
            if (id != 0) _events.Enqueue(id);
            while (_events.Count > MaximumTrackedEvents) Stop(_events.Dequeue());
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "[SupplyBox] Не удалось воспроизвести сигнал {SoundEvent}", name);
        }
    }

    public void StopAll()
    {
        while (_events.TryDequeue(out var id)) Stop(id);
    }

    private void Stop(uint id)
    {
        try
        {
            using var stop = core.NetMessage.Create<CMsgSosStopSoundEvent>();
            stop.SoundeventGuid = unchecked((int)id);
            stop.SendToAllPlayers();
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "[SupplyBox] Не удалось остановить сигнал сброса {SoundId}", id);
        }
    }
}
