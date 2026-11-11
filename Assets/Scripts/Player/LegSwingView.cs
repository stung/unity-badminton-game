using UnityEngine;

/// <summary>
/// Swings the legs back and forth while the character moves on the ground, as a rough "I am moving"
/// animation. Procedural and driven by actual speed, so it also covers dashes and bots, and needs no
/// Animator. Presentation only: nothing reads from this.
/// Each leg is a hip pivot at the top of the leg, rotated around its local X axis.
/// During a skid the legs stop trotting and brace instead: all four plant with the feet pushed out
/// in the slide direction, as a dog digs in to stop.
/// </summary>
public class LegSwingView : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    [Tooltip("Front-left and back-right. Moves opposite to the other pair, like a trot.")]
    [SerializeField] private Transform[] diagonalA = new Transform[0];
    [Tooltip("Front-right and back-left.")]
    [SerializeField] private Transform[] diagonalB = new Transform[0];

    [Tooltip("How far each leg swings from straight down at top speed (degrees).")]
    [SerializeField] private float maxSwingAngle = 35f;
    [Tooltip("Full back-and-forth swings per second at top speed.")]
    [SerializeField] private float swingsPerSecond = 3.5f;
    [Tooltip("How fast the legs paddle in the air, as a fraction of top-speed swinging. " +
             "A set rate, so they also move in a jump straight up.")]
    [SerializeField] private float airSwingSpeed = 1f;
    [Tooltip("How fast the swing grows and settles (fraction of full swing per second). " +
             "Stops the legs snapping straight the moment the character stops.")]
    [SerializeField] private float settleRate = 8f;

    [Header("Skid brace")]
    [Tooltip("How far the legs plant toward the slide at full skid severity (degrees).")]
    [SerializeField] private float braceAngle = 40f;
    [Tooltip("How fast the legs go into and out of the brace (fraction of full brace per second).")]
    [SerializeField] private float braceRate = 12f;

    private float phase;
    private float swingAmount;
    private float braceAmount;
    private float skidSeverity;
    private Vector3 braceDirection = Vector3.forward; // local-space slide direction while bracing

    private void OnEnable() => motor.Skidded += OnSkidded;
    private void OnDisable() => motor.Skidded -= OnSkidded;
    private void OnSkidded(float severity) => skidSeverity = severity;

    private void LateUpdate()
    {
        Vector3 velocity = motor.Velocity;
        velocity.y = 0f;
        // In the air the velocity is just momentum from takeoff, so it doesn't drive the legs.
        float speedFraction = motor.IsAirborne
            ? airSwingSpeed
            : motor.MaxSpeed > 0f ? velocity.magnitude / motor.MaxSpeed : 0f;

        // Game time, so the legs freeze with everything else during pause and hitstop. Not clamped
        // here, so a dash (faster than top speed) gives a quicker scramble.
        float dt = Time.deltaTime;
        phase = Mathf.Repeat(phase + speedFraction * swingsPerSecond * dt, 1f);
        swingAmount = Mathf.MoveTowards(swingAmount, Mathf.Clamp01(speedFraction), settleRate * dt);

        bool bracing = motor.IsSkidding && !motor.IsAirborne;
        if (bracing && velocity.sqrMagnitude > 0.0001f)
        {
            // In the legs' parent space, so the brace follows the body's lean.
            Vector3 local = LegParent != null ? LegParent.InverseTransformDirection(velocity) : velocity;
            local.y = 0f;
            if (local.sqrMagnitude > 0.0001f) braceDirection = local.normalized;
        }
        braceAmount = Mathf.MoveTowards(braceAmount, bracing ? skidSeverity : 0f, braceRate * dt);
        // Rotating about (down x direction) swings the foot (which hangs straight down) toward that direction.
        Quaternion brace = Quaternion.AngleAxis(braceAngle * braceAmount, Vector3.Cross(Vector3.down, braceDirection));
        float braceBlend = Mathf.Clamp01(braceAmount * 3f); // fully braced well before full severity

        float angle = Mathf.Sin(phase * 2f * Mathf.PI) * maxSwingAngle * swingAmount;
        SetPose(diagonalA, Quaternion.Euler(angle, 0f, 0f), brace, braceBlend);
        SetPose(diagonalB, Quaternion.Euler(-angle, 0f, 0f), brace, braceBlend);
    }

    private Transform LegParent => diagonalA.Length > 0 && diagonalA[0] != null ? diagonalA[0].parent : null;

    private static void SetPose(Transform[] legs, Quaternion swing, Quaternion brace, float braceBlend)
    {
        Quaternion pose = Quaternion.Slerp(swing, brace, braceBlend);
        foreach (Transform leg in legs)
        {
            if (leg != null) leg.localRotation = pose;
        }
    }
}
