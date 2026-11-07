using UnityEngine;

/// <summary>
/// A shot type as data. Adding a new shot should mean adding an asset (and a ShotTable rule), not code.
/// </summary>
[CreateAssetMenu(fileName = "Shot", menuName = "Badminton/Shot")]
public class ShotData : ScriptableObject
{
    public enum AimMode
    {
        /// <summary>Always launch at `speed` and `launchAngleDeg`. Depth then depends on where you hit from.</summary>
        FixedPower,
        /// <summary>Fixed angle; solve the speed that lands at `targetDepth` (clears, lifts, drops, net shots).</summary>
        TargetDepth,
        /// <summary>Fixed `speed`; solve the angle that lands at `targetDepth` (smashes, drives: pace is the shot).</summary>
        FixedSpeed,
    }

    [Header("Identity")]
    [Tooltip("Shown in the debug readout. Falls back to the asset name if empty.")]
    [SerializeField] private string displayName;
    [Tooltip("If true, the receiver's block / lift return rules apply to this shot.")]
    [SerializeField] private bool countsAsSmash;

    [Header("Launch")]
    [Tooltip("Elevation above horizontal, in degrees. TargetDepth: starting angle (raised/lowered to clear the net). " +
             "FixedSpeed: starting angle for the take-pace-off fallback.")]
    [SerializeField] private float launchAngleDeg = 40f;
    [SerializeField] private AimMode aimMode = AimMode.TargetDepth;
    [Tooltip("FixedPower / FixedSpeed: launch speed in m/s.")]
    [SerializeField] private float speed = 18f;
    [Tooltip("TargetDepth: where to land, in metres past the net on the opponent's side (baseline is at 6.7).")]
    [SerializeField] private float targetDepth = 5.9f;
    [Tooltip("TargetDepth (and the FixedSpeed fallback): speed cap (m/s). If the target is out of reach, " +
             "the shot goes at this speed and falls short.")]
    [SerializeField] private float maxSpeed = 100f;
    [Tooltip("Minimum height (m) above the net tape. If the arc is lower, the launch angle is " +
             "raised in 5° steps until it clears (perfect shot quality for now, so shots never hit the net).")]
    [SerializeField] private float minNetClearance = 0.1f;

    [Header("Fixed speed (smashes, drives)")]
    [Tooltip("Steepest angle tried (negative = downward).")]
    [SerializeField] private float minSolveAngleDeg = -40f;
    [Tooltip("Flattest/highest angle tried. If the shot still can't land in court at full speed, " +
             "it takes pace off (solves speed at launchAngleDeg instead).")]
    [SerializeField] private float maxSolveAngleDeg = 15f;

    [Header("Impact feedback")]
    [Tooltip("Hitstop: how long (seconds) the whole game freezes on contact. Telegraphs strong shots to both players.")]
    [SerializeField] private float hitstopSeconds = 0f;
    [Tooltip("Camera shake strength on contact, 0..1.")]
    [Range(0f, 1f)] [SerializeField] private float shakeIntensity = 0f;

    [Header("Aim")]
    [Tooltip("Sideways stick past this (0..1) at contact aims for a corner; below it the shot goes straight. " +
             "Keeps slight stick drift while running from turning a straight shot into a corner shot.")]
    [SerializeField] private float sideAimThreshold = 0.3f;
    [Tooltip("Landing point is kept at least this far inside the sidelines.")]
    [SerializeField] private float sidelineMargin = 0.3f;
    [Tooltip("FixedSpeed: a full-speed shot landing deeper than this far inside the baseline takes pace off instead.")]
    [SerializeField] private float baselineMargin = 0.3f;

    [Header("Cost")]
    [Tooltip("Not used yet; reserved for the stamina system.")]
    [SerializeField] private float staminaCost = 0f;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public bool CountsAsSmash => countsAsSmash;
    public float StaminaCost => staminaCost;
    public float HitstopSeconds => hitstopSeconds;
    public float ShakeIntensity => shakeIntensity;

    /// <summary>
    /// 0..1 multiplier for impact feedback, from how hard the shot actually went. A FixedSpeed shot that
    /// had to take pace off (e.g. a "smash" from a low contact) shouldn't freeze and shake like a full one.
    /// </summary>
    public float ImpactScale(float launchSpeed)
    {
        if (aimMode != AimMode.FixedSpeed || speed <= 0f) return 1f;
        return Mathf.Clamp01(launchSpeed / speed);
    }

    /// <param name="from">Contact point.</param>
    /// <param name="towardNet">Horizontal world direction from the hitter's side toward the net.</param>
    /// <param name="aimX">-1..1 sideways stick at contact, relative to the hitter facing the net (+1 = hitter's right).</param>
    /// <param name="court">For the net and sidelines. May be null (straight shots only, no net check).</param>
    public Vector3 GetLaunchVelocity(Vector3 from, Vector3 towardNet, float aimX, CourtDimensions court,
                                     in BirdieFlightParams flight)
    {
        towardNet.y = 0f;
        towardNet = towardNet.normalized;

        if (aimMode == AimMode.TargetDepth)
        {
            Vector3 target = ChooseTarget(from, towardNet, aimX, court);
            // This is the hook where future imprecision (stamina, positioning, timing) can nudge the
            // intended target and speed before solving. Shots are perfect for now.
            return LaunchSolver.SolveClearingNet(from, target, launchAngleDeg, minNetClearance, flight, court, maxSpeed);
        }

        if (aimMode == AimMode.FixedSpeed)
        {
            Vector3 target = ChooseTarget(from, towardNet, aimX, court);
            Vector3 velocity = SolveAtSpeed(from, target, flight, court);
            if (court != null)
            {
                // Clearing the net usually makes a fast shot land deeper than its target. On a diagonal
                // (corner aim) that extra depth also carries it sideways, past the sideline. So re-aim at
                // the same sideline spot but at the depth it actually lands; this converges in 1-2 passes.
                for (int pass = 0; pass < 3; pass++)
                {
                    LandingPrediction landing = LandingPredictor.Predict(
                        new BirdieSimState { Position = from, Velocity = velocity }, flight, court);
                    if (!landing.Valid || landing.HitNet) break;
                    float depthError = Vector3.Dot(landing.Point - target, towardNet);
                    if (Mathf.Abs(depthError) < 0.1f) break;
                    // towardNet is along z, so this moves the target's depth and keeps its sideline x.
                    target += towardNet * depthError;
                    velocity = SolveAtSpeed(from, target, flight, court);
                }
            }
            if (court == null || LandsInCourt(from, velocity, towardNet, court, flight)) return velocity;
            // At full pace it can't land in (e.g. a smash from a low contact must go flat to clear the
            // net, then sails long). A real player takes pace off: solve the speed for the target instead.
            return LaunchSolver.SolveClearingNet(from, target, launchAngleDeg, minNetClearance, flight, court, maxSpeed);
        }

        // FixedPower: no target, so aim by direction: straight, or toward the chosen sideline.
        Vector3 horizontal = towardNet;
        if (Mathf.Abs(aimX) > sideAimThreshold)
        {
            horizontal = (towardNet + HitterRight(towardNet) * Mathf.Sign(aimX)).normalized;
        }
        float elevation = launchAngleDeg * Mathf.Deg2Rad;
        Vector3 direction = horizontal * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);
        return direction * speed;
    }

    /// <summary>
    /// Like a straight shot, but landing at a chosen sideways position, for launches aimed at a
    /// player rather than steered by a stick. Uses the TargetDepth solve, whatever the aim mode.
    /// </summary>
    public Vector3 GetLaunchVelocityToward(Vector3 from, Vector3 towardNet, float targetX, CourtDimensions court,
                                           in BirdieFlightParams flight)
    {
        towardNet.y = 0f;
        towardNet = towardNet.normalized;
        Vector3 target = ChooseTarget(from, towardNet, 0f, court);
        target.x = targetX;
        return LaunchSolver.SolveClearingNet(from, target, launchAngleDeg, minNetClearance, flight, court, maxSpeed);
    }

    /// <summary>
    /// Players aim at a spot, not an angle: straight ahead from where they stand, or the corner on
    /// their left/right, always at this shot's target depth. Aiming at points means a short net drop
    /// can still cross the whole court.
    /// </summary>
    private Vector3 ChooseTarget(Vector3 from, Vector3 towardNet, float aimX, CourtDimensions court)
    {
        // "Depth past the net" is the position projected on towardNet, which works for either side.
        Vector3 target = from + towardNet * (targetDepth - Vector3.Dot(from, towardNet));
        target.y = 0f;
        if (court == null) return target;

        // The net runs along x for both sides, so the sidelines are always at x = ±limit.
        float limit = Mathf.Max(0f, court.HalfWidth - sidelineMargin);
        if (Mathf.Abs(aimX) > sideAimThreshold)
        {
            // The hitter's right is +x on the near side and -x on the far side.
            target.x = Mathf.Sign(HitterRight(towardNet).x * aimX) * limit;
        }
        else
        {
            target.x = Mathf.Clamp(target.x, -limit, limit);
        }
        return target;
    }

    private Vector3 SolveAtSpeed(Vector3 from, Vector3 target, in BirdieFlightParams flight, CourtDimensions court)
    {
        return LaunchSolver.SolveAngleAtSpeed(from, target, speed, minSolveAngleDeg, maxSolveAngleDeg,
                                              minNetClearance, flight, court);
    }

    private bool LandsInCourt(Vector3 from, Vector3 velocity, Vector3 towardNet, CourtDimensions court,
                              in BirdieFlightParams flight)
    {
        LandingPrediction landing = LandingPredictor.Predict(
            new BirdieSimState { Position = from, Velocity = velocity }, flight, court);
        if (!landing.Valid || landing.HitNet) return false;
        float depthPastNet = Vector3.Dot(landing.Point, towardNet);
        return depthPastNet > 0f
               && depthPastNet <= court.HalfLength - baselineMargin
               && Mathf.Abs(landing.Point.x) <= court.HalfWidth;
    }

    /// <summary>The hitter's right when facing the net (up × forward).</summary>
    private static Vector3 HitterRight(Vector3 towardNet) => Vector3.Cross(Vector3.up, towardNet);
}
