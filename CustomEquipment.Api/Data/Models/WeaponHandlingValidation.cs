namespace CustomEquipment.Api.Data.Models;

internal static class WeaponHandlingValidation
{
    internal static void ValidateModes(
        IReadOnlyList<float>? values,
        string field,
        float min = 0f,
        float max = float.MaxValue)
    {
        if (values is null || values.Count > 2)
        {
            throw new ArgumentException("Expected an array with at most two firing modes.", field);
        }

        foreach (var value in values)
        {
            ValidateValue(value, field, min, max);
        }
    }

    internal static void ValidateValue(float? value, string field, float min = 0f, float max = float.MaxValue)
    {
        if (value.HasValue && (!float.IsFinite(value.Value) || value < min || value > max))
        {
            throw new ArgumentOutOfRangeException(field, $"Expected a finite value between {min} and {max}.");
        }
    }
}
