using UnityEngine;

/// <summary>
/// Moves a character kinematically (no Rigidbody) with acceleration/deceleration,
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

    private CourtSide courtSide;
    private Vector3 velocity;

    public Vector3 Velocity => velocity;
    public CourtSide CourtSide => courtSide;

    private void Awake()
    {
        courtSide = GetComponent<CourtSide>();
        transform.rotation = Quaternion.LookRotation(courtSide.TowardNet, Vector3.up);
    }

    /// <param name="move">Side-relative move intent, magnitude 0..1.</param>
    /// <param name="speedMultiplier">Lets states (e.g. swinging) slow the character down.</param>
    public void Tick(Vector2 move, float speedMultiplier, float dt)
    {
        Vector3 targetVelocity = courtSide.ToWorld(move) * (maxSpeed * speedMultiplier);

        // Decelerate when there's no input, otherwise accelerate toward the target velocity.
        float rate = move.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * dt);

        Vector3 desired = transform.position + velocity * dt;
        Vector3 clamped = courtSide.ClampToHalf(desired);

        // Kill velocity along any axis we got clamped on, so we don't "stick" to the boundary.
        if (!Mathf.Approximately(clamped.x, desired.x)) velocity.x = 0f;
        if (!Mathf.Approximately(clamped.z, desired.z)) velocity.z = 0f;

        transform.position = clamped;
    }
}
