using UnityEngine;

/// <summary>
/// Slows the whole game slightly while any character is in the air from a jump AND the birdie is
/// about to come into their reach, so a leaping hit gets a readable, dramatic beat. Jumps with no
/// birdie nearby get nothing. It's global (both dogs and the birdie slow together), and it checks
/// every character the same way, so the bot gets it too once it can jump.
/// </summary>
public class JumpSlowMo : MonoBehaviour
{
    [SerializeField] private GameTime gameTime;
    [Tooltip("Everyone who can trigger it. Each needs a CharacterMotor on the same object.")]
    [SerializeField] private ShotHitter[] hitters;

    [SerializeField] private bool slowMoEnabled = true;
    [Tooltip("Game speed during slow-mo (0..1). 1 = no slow-down.")]
    [Range(0.05f, 1f)] [SerializeField] private float slowScale = 0.5f;
    [Tooltip("Game seconds ahead to look for the birdie entering reach. Bigger = slow-mo starts earlier.")]
    [SerializeField] private float lookAheadSeconds = 0.25f;
    [Tooltip("Real seconds to ease from full speed down to slowScale.")]
    [SerializeField] private float easeInSeconds = 0.08f;
    [Tooltip("Real seconds to ease back up to full speed. A little longer than easing in, so it doesn't snap back.")]
    [SerializeField] private float easeOutSeconds = 0.15f;

    private CharacterMotor[] motors;
    private float dilation = 1f;

    public bool IsSlowing { get; private set; }

    private void Awake()
    {
        motors = new CharacterMotor[hitters.Length];
        for (int i = 0; i < hitters.Length; i++) motors[i] = hitters[i] != null ? hitters[i].GetComponent<CharacterMotor>() : null;
    }

    private void Update()
    {
        // Hold the blend while paused, so slow-mo resumes exactly where it left off.
        if (gameTime.IsPaused) return;

        IsSlowing = slowMoEnabled && AnyJumpAboutToHit();
        dilation = TimeDilationBlend.Step(dilation, IsSlowing, slowScale, easeInSeconds, easeOutSeconds,
                                          Time.unscaledDeltaTime);
        gameTime.SetDilation(dilation);
    }

    private bool AnyJumpAboutToHit()
    {
        for (int i = 0; i < hitters.Length; i++)
        {
            if (hitters[i] == null || motors[i] == null) continue;
            // Airborne is checked first: it's free, while the look-ahead steps the flight sim.
            if (TimeDilationBlend.ShouldSlow(motors[i].IsAirborne, motors[i].IsAirborne && hitters[i].WillBeInReachWithin(lookAheadSeconds)))
                return true;
        }
        return false;
    }

    private void OnDisable()
    {
        // Never leave the game stuck in slow-mo.
        dilation = 1f;
        IsSlowing = false;
        if (gameTime != null) gameTime.SetDilation(1f);
    }
}
