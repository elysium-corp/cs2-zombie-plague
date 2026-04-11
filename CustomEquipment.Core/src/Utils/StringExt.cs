namespace CustomEquipment.Utils;

internal static class StringExt
{
    extension(string? value)
    {
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public bool IsNotNullOrEmpty()
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}