namespace Pheidi.Common.Engines;

public static class SpikeGuard
{
    /// <summary>
    /// Returns the maximum safe distance for a single session: 110% of the max
    /// from the provided recent distances.
    /// </summary>
    public static decimal GetMaxSafeDistance(decimal[] recentDistances)
    {
        if (recentDistances.Length == 0) return decimal.MaxValue;
        return recentDistances.Max() * 1.10m;
    }

    /// <summary>
    /// Checks whether a workout distance exceeds 110% of the longest completed
    /// run in the provided history.
    /// </summary>
    public static bool IsSpike(decimal workoutDistance, decimal[] recentCompletedDistances)
    {
        if (recentCompletedDistances.Length == 0) return false;
        return workoutDistance > GetMaxSafeDistance(recentCompletedDistances);
    }
}
