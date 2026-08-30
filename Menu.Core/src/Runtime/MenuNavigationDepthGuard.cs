namespace Menu.Core.Runtime;

internal static class MenuNavigationDepthGuard
{
    public static bool TryAdvance(int currentDepth, int maximumDepth, out int nextDepth)
    {
        if (currentDepth < 0 || maximumDepth < 1 || currentDepth >= maximumDepth)
        {
            nextDepth = currentDepth;
            return false;
        }

        nextDepth = currentDepth + 1;
        return true;
    }

    public static int Back(int currentDepth)
    {
        return Math.Max(0, currentDepth - 1);
    }
}
