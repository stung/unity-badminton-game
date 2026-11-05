using UnityEngine;

/// <summary>
/// A shot type as data. Adding a new shot should mean adding an asset (and a ShotTable rule), not code.
/// </summary>
[CreateAssetMenu(fileName = "Shot", menuName = "Badminton/Shot")]
public class ShotData : ScriptableObject
{
    public enum AimMode
    {
        /// <summary>Always launch at `speed`. Depth then depends on where you hit from.</summary>
        FixedPower,
        /// <summary>Solve for the speed that lands at `targetDepth`, like a player adjusting their swing.</summary>
        TargetDepth,
    }

    [Header("Identity")]
    [Tooltip("Shown in the debug readout. Falls back to the asset name if empty.")]
    [SerializeField] private string displayName;
    [Tooltip("If true, the receiver's block / lift return rules apply to this shot.")]
    [SerializeField] private bool countsAsSmash;

    [Header("Launch")]
    [Tooltip("Elevation above horizontal, in degrees. TargetDepth mode may raise it to clear the net.")]
    [SerializeField] private float launchAngleDeg = 40f;
    [SerializeField] private AimMode aimMode = AimMode.TargetDepth;
    [Tooltip("FixedPower: launch speed in m/s.")]
    [SerializeField] private float speed = 18f;
    [Tooltip("TargetDepth: where to land, in metres past the net on the opponent's side (baseline is at 6.7).")]
    [SerializeField] private float targetDepth = 5.9f;
    [Tooltip("TargetDepth: speed cap (m/s). If the target is out of reach, the shot goes at this speed and falls short.")]
    [SerializeField] private float maxSpeed = 100f;
    [Tooltip("TargetDepth: minimum height (m) above the net tape. If the arc is lower, the launch angle is " +
             "raised in 5° steps until it clears (perfect shot quality for now, so shots never hit the net).")]
    [SerializeField] private float minNetClearance = 0.1f;

    [Header("Aim")]
    [Tooltip("Sideways stick past this (0..1) at contact aims for a corner; below it the shot goes straight. " +
             "Keeps slight stick drift while running from turning a straight shot into a corner shot.")]
    [SerializeField] private float sideAimThreshold = 0.3f;
    [Tooltip("Landing point is kept at least this far inside the sidelines.")]
    [SerializeField] private float sidelineMargin = 0.3f;

    [Header("Cost")]
    [Tooltip("Not used yet; reserved for the stamina system.")]
    [SerializeField] private float staminaCost = 0f;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public bool CountsAsSmash => countsAsSmash;
    public float StaminaCost => staminaCost;

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

    /// <summary>The hitter's right when facing the net (up × forward).</summary>
    private static Vector3 HitterRight(Vector3 towardNet) => Vector3.Cross(Vector3.up, towardNet);
}
