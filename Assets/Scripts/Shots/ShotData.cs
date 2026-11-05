using UnityEngine;

/// <summary>
/// A shot type as data. Adding a new shot (drop, smash, ...) should mean adding an asset, not code.
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

    [Header("Launch")]
    [Tooltip("Elevation above horizontal, in degrees. Clears are high and steep.")]
    [SerializeField] private float launchAngleDeg = 40f;
    [SerializeField] private AimMode aimMode = AimMode.TargetDepth;
    [Tooltip("FixedPower: launch speed in m/s. With heavy drag, distance grows slowly with speed, so tune in small steps.")]
    [SerializeField] private float speed = 18f;
    [Tooltip("TargetDepth: where to land, in metres past the net on the opponent's side (baseline is at 6.7).")]
    [SerializeField] private float targetDepth = 5.9f;
    [Tooltip("TargetDepth: speed cap (m/s). If the target is out of reach, the shot goes at this speed and falls short.")]
    [SerializeField] private float maxSpeed = 100f;
    [Tooltip("Max left/right aim (degrees) from full sideways stick at the moment of contact.")]
    [SerializeField] private float maxAimYawDeg = 12f;

    [Header("Cost")]
    [Tooltip("Not used yet; reserved for the stamina system.")]
    [SerializeField] private float staminaCost = 0f;

    [Header("Swing timing (seconds)")]
    [Tooltip("Time from button press until the racket can make contact.")]
    [SerializeField] private float windup = 0.05f;
    [Tooltip("How long contact is possible. Longer = more forgiving timing.")]
    [SerializeField] private float activeWindow = 0.25f;
    [Tooltip("Time after the active window before the character can act again.")]
    [SerializeField] private float recovery = 0.25f;
    [Tooltip("Movement speed multiplier while swinging/recovering.")]
    [SerializeField] private float swingMoveMultiplier = 0.3f;

    public float StaminaCost => staminaCost;
    public float Windup => windup;
    public float ActiveWindow => activeWindow;
    public float Recovery => recovery;
    public float SwingMoveMultiplier => swingMoveMultiplier;

    /// <param name="from">Contact point.</param>
    /// <param name="towardNet">Horizontal world direction from the hitter's side toward the net.</param>
    /// <param name="aimX">-1..1 sideways aim, relative to the hitter facing the net.</param>
    public Vector3 GetLaunchVelocity(Vector3 from, Vector3 towardNet, float aimX, in BirdieFlightParams flight)
    {
        towardNet = towardNet.normalized;
        // Yaw positive = clockwise seen from above = to the hitter's right when facing the net.
        Vector3 horizontal = Quaternion.AngleAxis(aimX * maxAimYawDeg, Vector3.up) * towardNet;

        if (aimMode == AimMode.TargetDepth)
        {
            // Walk along the aim direction until we're targetDepth past the net. "Depth past the net"
            // is the position projected on towardNet, which works for either side of the court.
            float distance = (targetDepth - Vector3.Dot(from, towardNet)) / Vector3.Dot(horizontal, towardNet);
            Vector3 target = from + horizontal * distance;
            target.y = 0f;
            // If out of range, the solver hands back the max-speed velocity, which is the best we can do.
            LaunchSolver.TrySolve(from, target, launchAngleDeg, flight, out Vector3 solved, maxSpeed: maxSpeed);
            return solved;
        }

        float elevation = launchAngleDeg * Mathf.Deg2Rad;
        Vector3 direction = horizontal * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);
        return direction * speed;
    }
}
