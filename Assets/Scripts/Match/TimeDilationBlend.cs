using UnityEngine;

/// <summary>
/// Pure rules for the pre-hit jump slow-mo: when to slow down, and how to ease the game speed
/// toward its target. No Unity objects, so Edit Mode tests can call it directly.
/// </summary>
public static class TimeDilationBlend
{
    /// <summary>Slow-mo only for a jump that's about to meet the birdie, never for jumps in general.</summary>
    public static bool ShouldSlow(bool airborne, bool birdieAboutToBeInReach) => airborne && birdieAboutToBeInReach;

    /// <summary>
    /// Moves the dilation toward its target at a constant rate, so a full ease between 1 and
    /// slowScale takes exactly easeInSeconds (slowing) or easeOutSeconds (speeding back up).
    /// A constant rate (rather than lerping a fraction per frame) makes the timing frame-rate independent
    /// and guarantees it actually arrives instead of creeping closer forever.
    /// </summary>
    /// <param name="dt">Real (unscaled) seconds: this controls the game clock, so it can't run on it.</param>
    public static float Step(float current, bool slow, float slowScale,
                             float easeInSeconds, float easeOutSeconds, float dt)
    {
        float target = slow ? slowScale : 1f;
        float span = Mathf.Abs(1f - slowScale);
        float seconds = target < current ? easeInSeconds : easeOutSeconds;
        if (seconds <= 0f || span <= 0f) return target;
        return Mathf.MoveTowards(current, target, span / seconds * dt);
    }
}
