using UnityEngine;

/// <summary>
/// Birdie sounds: the racket hit (louder and a little higher the harder the shot) and the landing
/// thud. Each shot can override the hit sound in its ShotData; otherwise the default plays.
/// Presentation only: listens to Birdie events, never affects the game.
/// </summary>
public class ShotAudio : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [SerializeField] private SfxPlayer sfx;
    [SerializeField] private SoundSet defaultHit;
    [SerializeField] private SoundSet land;

    [Tooltip("Launch speed (m/s) that counts as a full-strength hit. A full back smash is ~90.")]
    [SerializeField] private float fullIntensitySpeed = 90f;
    [Tooltip("Volume of the softest hits, as a fraction of the hardest.")]
    [Range(0f, 1f)] [SerializeField] private float quietFraction = 0.55f;
    [Tooltip("How much higher the hardest hits sound (0.15 = 15%).")]
    [SerializeField] private float pitchBoost = 0.15f;
    [Tooltip("How far sounds pan toward the side of the court they happen on (0 = centre, 1 = fully).")]
    [Range(0f, 1f)] [SerializeField] private float panAmount = 0.4f;

    private void OnEnable()
    {
        birdie.Launched += OnLaunched;
        birdie.Landed += OnLanded;
    }

    private void OnDisable()
    {
        birdie.Launched -= OnLaunched;
        birdie.Landed -= OnLanded;
    }

    private void OnLaunched(LandingPrediction prediction)
    {
        // Feeds and test launches have no shot; they still get the default sound.
        ShotData shot = birdie.LastShot;
        SoundSet set = shot != null && shot.HitSound != null ? shot.HitSound : defaultHit;
        float intensity = SoundRules.ImpactIntensity(birdie.LaunchSpeed, fullIntensitySpeed);
        sfx.Play(set, SoundRules.Volume(intensity, quietFraction), SoundRules.Pitch(intensity, pitchBoost),
                 Pan(birdie.Position.x));
    }

    private void OnLanded(Vector3 point) => sfx.Play(land, 1f, 1f, Pan(point.x));

    private float Pan(float x) => SoundRules.Pan(x, birdie.Court != null ? birdie.Court.HalfWidth : 0f, panAmount);
}
