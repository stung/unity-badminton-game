using UnityEngine;

/// <summary>
/// Triggers hitstop when the birdie is struck, scaled by the shot's strength, so both players
/// get a beat to see a strong shot coming. Works for any hitter (player, bot, feeder).
/// </summary>
public class HitstopOnImpact : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [SerializeField] private GameTime gameTime;
    [Tooltip("Global multiplier on every shot's hitstop. 0 disables hitstop without editing shot assets.")]
    [SerializeField] private float hitstopScale = 1f;

    private void OnEnable()
    {
        if (birdie != null) birdie.Launched += OnLaunched;
    }

    private void OnDisable()
    {
        if (birdie != null) birdie.Launched -= OnLaunched;
    }

    private void OnLaunched(LandingPrediction prediction)
    {
        ShotData shot = birdie.LastShot;
        if (shot != null) gameTime.Hitstop(shot.HitstopSeconds * shot.ImpactScale(birdie.LaunchSpeed) * hitstopScale);
    }
}
