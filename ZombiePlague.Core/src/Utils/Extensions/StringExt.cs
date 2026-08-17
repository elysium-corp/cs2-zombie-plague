namespace ZombiePlague.Core.Utils.Extensions;

internal static class StringExt
{
    extension(string value)
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
    
    extension(ICollection<String> collection)
    {
        public String GetRandomString()
        {
            return collection.Count > 0 ? collection.ElementAt(Numeric.Random(0, collection.Count)) : "";
        }
    }
}