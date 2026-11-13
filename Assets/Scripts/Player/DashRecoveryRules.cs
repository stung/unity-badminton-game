/// <summary>
/// Pure rules for dash recovery, kept out of the MonoBehaviour so Edit Mode tests can check them
/// without loading a scene.
/// </summary>
public static class DashRecoveryRules
{
    /// <summary>
    /// Whether a shot press registers at this point in the dash recovery. A shot is the only input
    /// recovery accepts, and only inside the hit window: diving at the birdie should be able to reach
    /// it, but not let you get up and play on freely.
    /// </summary>
    /// <param name="timeInRecovery">Seconds since recovery started.</param>
    /// <param name="windowStart">Seconds into recovery the hit window opens.</param>
    /// <param name="windowLength">Seconds the hit window stays open.</param>
    public static bool CanHit(float timeInRecovery, float windowStart, float windowLength)
    {
        return windowLength > 0f
            && timeInRecovery >= windowStart
            && timeInRecovery < windowStart + windowLength;
    }
}
