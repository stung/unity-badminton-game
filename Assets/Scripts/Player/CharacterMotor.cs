using UnityEngine;

/// <summary>
/// Moves a character kinematically (no Rigidbody) with acceleration/deceleration, dashes and jumps,
/// clamped to its half of the court. Starts facing the net; add CharacterFacing to turn after that.
/// </summary>
[RequireComponent(typeof(CourtSide))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [Tooltip("m/s² when the stick is released. Higher = snappier stops.")]
    [SerializeField] private float deceleration = 40f;

    [Header("Speed buildup")]
    // Two phases so the first step stays snappy (reflex feel) but top speed takes a moment, which
    // makes repositioning cost something. With the defaults: ~0.08 s to 60% speed, ~0.4 s to full.
    [Tooltip("Fraction of top speed (0..1) reached by the quick burst before the slower build takes over.")]
    [Range(0f, 1f)] [SerializeField] private float burstFraction = 0.6f;
    [Tooltip("m/s² from standstill up to the burst speed.")]
    [SerializeField] private float burstAcceleration = 40f;
    [Tooltip("m/s² from the burst speed up to top speed. Lower = longer to reach full speed.")]
    [SerializeField] private float buildAcceleration = 6f;

    [Header("Turnaround (skid)")]
    // Turning sharply means braking off the momentum that's heading the wrong way before building
    // speed the new way. A fixed braking rate makes the cost grow with speed: reversing at top speed
    // (5 m/s) skids ~0.28 s, at 2 m/s only ~0.11 s, then the speed buildup starts over.
    [Tooltip("Turns up to this angle (degrees) from the current heading keep their speed.")]
    [Range(0f, 90f)] [SerializeField] private float freeTurnAngle = 45f;
    [Tooltip("Degrees per second a free turn can swing the heading. Stops a fast stick sweep from dodging the skid.")]
    [SerializeField] private float freeTurnRate = 540f;
    [Tooltip("m/s² at which wrong-way momentum brakes off in a sharp turn. Lower = longer, costlier skids.")]
    [SerializeField] private float skidDeceleration = 18f;
    [Tooltip("Wrong-way speed (m/s) needed to count as a skid. Below this a turn is a quick shuffle: " +
             "it still brakes, but doesn't block dashing or trigger skid visuals.")]
    [SerializeField] private float skidMinSpeed = 1.5f;

    [Header("Dash")]
    [Tooltip("How far a dash travels (m).")]
    [SerializeField] private float dashDistance = 1.5f;
    [Tooltip("How long a dash takes (s).")]
    [SerializeField] private float dashDuration = 0.12f;
    [Tooltip("Stick deflection (0..1) needed to aim a dash. Below this, it goes the way the character faces.")]
    [SerializeField] private float dashAimThreshold = 0.2f;

    [Header("Jump")]
    [Tooltip("Height of the jump's peak (m).")]
    [SerializeField] private float jumpHeight = 0.8f;
    [Tooltip("Time from takeoff to landing (s).")]
    [SerializeField] private float jumpDuration = 0.6f;

    private CourtSide courtSide;
    private Vector3 velocity;
    private float groundHeight;
    private Vector3 startPosition;

    private Vector3 dashDirection;
    private float dashDistanceLeft;
    private float airTime;

    public Vector3 Velocity => velocity;
    public CourtSide CourtSide => courtSide;
    public float MaxSpeed => maxSpeed;
    public float DashDuration => dashDuration;
    public bool IsAirborne { get; private set; }
    public MovementTuning Tuning => new MovementTuning
    {
        MaxSpeed = maxSpeed,
        BurstFraction = burstFraction,
        BurstAcceleration = burstAcceleration,
        BuildAcceleration = buildAcceleration,
        Deceleration = deceleration,
        FreeTurnAngle = freeTurnAngle,
        FreeTurnRate = freeTurnRate,
        SkidDeceleration = skidDeceleration,
    };
    /// <summary>
    /// True while braking off wrong-way momentum after a sharp turn. Dashing is blocked meanwhile, so a
    /// dash can't be used as a cheap way out of the turnaround penalty.
    /// </summary>
    public bool IsSkidding { get; private set; }
    /// <summary>
    /// Fired when a skid starts. Arg: severity 0..1, the wrong-way speed as a fraction of top speed
    /// (a full reversal at top speed is ~1). For visuals and sound; game logic doesn't need it.
    /// </summary>
    public event System.Action<float> Skidded;
    /// <summary>How far above the ground the character is (m). 0 unless jumping.</summary>
    public float HeightAboveGround => transform.position.y - groundHeight;

    private void Awake()
    {
        courtSide = GetComponent<CourtSide>();
        groundHeight = transform.position.y;
        startPosition = transform.position;
        transform.rotation = Quaternion.LookRotation(courtSide.TowardNet, Vector3.up);
    }

    /// <summary>Back to the starting spot, still and facing the net.</summary>
    public void ResetToStart() => ResetTo(startPosition);

    /// <summary>Moves to a spot on the ground, still and facing the net.</summary>
    public void ResetTo(Vector3 position)
    {
        velocity = Vector3.zero;
        IsAirborne = false;
        IsSkidding = false;
        dashDistanceLeft = 0f;
        position.y = groundHeight;
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(courtSide.TowardNet, Vector3.up);
    }

    /// <param name="move">Side-relative move intent, magnitude 0..1.</param>
    /// <param name="speedMultiplier">Lets states (e.g. swinging) slow the character down.</param>
    public void Tick(Vector2 move, float speedMultiplier, float dt)
    {
        if (IsAirborne)
        {
            // No air control: momentum is fixed at takeoff, so input and speed multipliers are ignored.
            TickJump(dt);
        }
        else
        {
            // Measured before stepping: the skid is about the momentum you had when you turned.
            bool wasSkidding = IsSkidding;
            velocity = MovementRules.Step(velocity, courtSide.ToWorld(move), speedMultiplier, Tuning, dt,
                                          wasSkidding, out float wrongWaySpeed);
            // Enter at skidMinSpeed but stay in until the wrong-way speed is fully gone, so a skid
            // doesn't flicker off (and re-allow dashing) partway through braking.
            IsSkidding = wasSkidding ? wrongWaySpeed > 0.01f : wrongWaySpeed >= skidMinSpeed;
            if (IsSkidding && !wasSkidding) Skidded?.Invoke(Mathf.Clamp01(wrongWaySpeed / Mathf.Max(maxSpeed, 0.0001f)));
        }

        MoveHorizontally(velocity * dt);
    }

    /// <returns>False if already in the air.</returns>
    public bool TryJump()
    {
        if (IsAirborne) return false;
        IsAirborne = true;
        airTime = 0f;
        return true;
    }

    /// <summary>Starts a dash toward the stick, or the way the character faces if the stick is near centre.</summary>
    /// <returns>The world direction of the dash.</returns>
    public Vector3 BeginDash(Vector2 move)
    {
        dashDirection = move.sqrMagnitude >= dashAimThreshold * dashAimThreshold
            ? courtSide.ToWorld(move.normalized)
            : FlatForward();
        dashDistanceLeft = dashDistance;
        IsSkidding = false;
        return dashDirection;
    }

    /// <returns>True when the dash has finished.</returns>
    public bool TickDash(float dt)
    {
        // Spend a fixed distance rather than running for a fixed time, so every dash travels exactly
        // dashDistance whatever the frame rate.
        float dashSpeed = dashDistance / Mathf.Max(dashDuration, 0.0001f);
        float step = Mathf.Min(dashSpeed * dt, dashDistanceLeft);
        dashDistanceLeft -= step;
        velocity = dashDirection * dashSpeed;
        MoveHorizontally(dashDirection * step);

        if (dashDistanceLeft > 0f) return false;
        // Come out at running speed, so the dash recovery that follows slides to a stop instead of
        // halting dead (an instant stop from dash speed would look like hitting a wall).
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        return true;
    }

    private void TickJump(float dt)
    {
        airTime += dt;
        float t = airTime / Mathf.Max(jumpDuration, 0.0001f);
        if (t >= 1f)
        {
            IsAirborne = false;
            t = 1f;
        }

        // A parabola from the elapsed time rather than stepped gravity, so the peak height and
        // air time are exact at any frame rate. At t = 0.5 this equals jumpHeight.
        Vector3 position = transform.position;
        position.y = groundHeight + 4f * jumpHeight * t * (1f - t);
        transform.position = position;
    }

    private void MoveHorizontally(Vector3 delta)
    {
        Vector3 desired = transform.position + delta;
        Vector3 clamped = courtSide.ClampToHalf(desired);

        // Kill velocity along any axis we got clamped on, so we don't "stick" to the boundary.
        if (!Mathf.Approximately(clamped.x, desired.x)) velocity.x = 0f;
        if (!Mathf.Approximately(clamped.z, desired.z)) velocity.z = 0f;

        transform.position = clamped;
    }

    private Vector3 FlatForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : courtSide.TowardNet;
    }
}
