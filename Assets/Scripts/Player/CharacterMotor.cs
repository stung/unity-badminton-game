using UnityEngine;

/// <summary>
/// Moves a character kinematically (no Rigidbody) with acceleration/deceleration, dashes and jumps,
/// clamped to its half of the court. Starts facing the net; add CharacterFacing to turn after that.
/// </summary>
[RequireComponent(typeof(CourtSide))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [Tooltip("m/s² when speeding up or changing direction.")]
    [SerializeField] private float acceleration = 30f;
    [Tooltip("m/s² when the stick is released. Higher = snappier stops.")]
    [SerializeField] private float deceleration = 40f;

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
    public bool IsAirborne { get; private set; }
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
            Vector3 targetVelocity = courtSide.ToWorld(move) * (maxSpeed * speedMultiplier);

            // Decelerate when there's no input, otherwise accelerate toward the target velocity.
            float rate = move.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * dt);
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
        // Come out at running speed, so a dash flows into a run instead of stopping dead.
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
