using UnityEngine;

/// <summary>
/// Pure mapping from what happened in the game to how loud, how high and where a sound plays.
/// No Unity objects, so Edit Mode tests can check it directly.
/// </summary>
public static class SoundRules
{
    /// <summary>
    /// 0..1 strength of a hit from how fast the birdie left the racket. Launch speed (not
    /// ShotData.ImpactScale, which is 1 for every placed shot) is what separates a soft lift from a smash.
    /// </summary>
    public static float ImpactIntensity(float launchSpeed, float fullIntensitySpeed)
        => fullIntensitySpeed > 0f ? Mathf.Clamp01(launchSpeed / fullIntensitySpeed) : 1f;

    /// <summary>Soft hits play at quietFraction of full volume, the hardest at full volume.</summary>
    public static float Volume(float intensity, float quietFraction)
        => Mathf.Lerp(quietFraction, 1f, Mathf.Clamp01(intensity));

    /// <summary>Harder hits sound slightly higher and sharper. pitchBoost 0.15 = up to 15% higher.</summary>
    public static float Pitch(float intensity, float pitchBoost)
        => 1f + pitchBoost * Mathf.Clamp01(intensity);

    /// <summary>
    /// Stereo pan (-1 left .. 1 right) from where on the court the sound happened. Kept partial
    /// (panAmount below 1) so nothing plays in one ear only.
    /// </summary>
    public static float Pan(float worldX, float courtHalfWidth, float panAmount)
        => courtHalfWidth > 0f ? Mathf.Clamp(worldX / courtHalfWidth, -1f, 1f) * panAmount : 0f;
}
