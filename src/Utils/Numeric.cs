namespace CS2ZombiePlague.Utils;

public static class Numeric
{
    private static readonly Random Randomizer = new();
    
    public static int Random(int min, int max)
    {
        return Randomizer.Next(min, max);
    }

    public static int Random(Range range)
    {
        return Randomizer.Next(range.Start.Value, range.End.Value);
    }
}