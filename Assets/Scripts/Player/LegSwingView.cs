using UnityEngine;

/// <summary>
/// Swings the legs back and forth while the character moves on the ground, as a rough "I am moving"
/// animation. Procedural and driven by actual speed, so it also covers dashes and bots, and needs no
/// Animator. Presentation only: nothing reads from this.
/// Each leg is a hip pivot at the top of the leg, rotated around its local X axis.
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

    private float phase;
    private float swingAmount;

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

        float angle = Mathf.Sin(phase * 2f * Mathf.PI) * maxSwingAngle * swingAmount;
        SetAngle(diagonalA, angle);
        SetAngle(diagonalB, -angle);
    }

    private static void SetAngle(Transform[] legs, float angle)
    {
        foreach (Transform leg in legs)
        {
            if (leg != null) leg.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
