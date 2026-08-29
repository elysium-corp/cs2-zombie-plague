namespace CustomEquipment.Policies;

internal static class LaserMinePolicy
{
    public static bool CanGrant(bool hasCarriedMine, bool grantInProgress)
    {
        return !hasCarriedMine && !grantInProgress;
    }

    public static bool CanUseC4(
        bool isLaserMine,
        bool grantInProgress,
        bool accessAllowed,
        bool hasOtherCarriedMine
    )
    {
        return grantInProgress ||
               isLaserMine && accessAllowed && !hasOtherCarriedMine;
    }

    public static bool ShouldRemoveC4(bool isLaserMine)
    {
        return !isLaserMine;
    }
}
