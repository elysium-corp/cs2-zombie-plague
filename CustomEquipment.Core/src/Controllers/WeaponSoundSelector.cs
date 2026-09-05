using CustomEquipment.Api.Data.Models;

namespace CustomEquipment.Controllers;

internal static class WeaponSoundSelector
{
    internal static WeaponSound? Select(
        IReadOnlyCollection<WeaponSound> sounds,
        string trigger,
        Random random)
    {
        var count = sounds.Count(sound =>
            string.Equals(sound.Trigger, trigger, StringComparison.OrdinalIgnoreCase));
        if (count == 0)
        {
            return null;
        }

        // Выбираем индекс только среди вариантов триггера, без массива на каждый выстрел.
        var index = random.Next(count);
        foreach (var sound in sounds)
        {
            if (string.Equals(sound.Trigger, trigger, StringComparison.OrdinalIgnoreCase) && index-- == 0)
            {
                return sound;
            }
        }

        return null;
    }
}
