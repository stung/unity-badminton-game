using UnityEngine;

/// <summary>
/// Sells a skid with the body: it leans back against the slide and squashes a little, then springs
/// upright with a small overshoot as the skid ends (squash and stretch: briefly deforming the body to
/// show weight and momentum). Both scale with skid severity, so small turns show nothing.
/// Presentation only: it moves a "Visual" child holding the body parts, never the character root, so
/// the reach area (HitPoint, a sibling of Visual) and game logic are untouched.
/// </summary>
public class SkidLeanView : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    [Tooltip("Pivot at the feet holding the body, face, legs and racket. Not the HitPoint.")]
    [SerializeField] private Transform visual;

    [Tooltip("Lean (degrees) at full severity: a reversal at top speed.")]
    [SerializeField] private float maxLeanAngle = 25f;
    [Tooltip("Height squash (fraction) at full severity. Width grows to match, so the volume looks kept.")]
    [Range(0f, 0.5f)] [SerializeField] private float maxSquash = 0.15f;
    [Tooltip("Spring stiffness. Higher = snappier lean in and out.")]
    [SerializeField] private float stiffness = 250f;
    [Tooltip("Spring damping. Lower = more overshoot (the 'spring forward' after a skid). " +
             "About 2 x sqrt(stiffness) means no overshoot at all.")]
    [SerializeField] private float damping = 18f;

    private float severity;
    private float amount;         // 0..1 of the full pose, driven by the spring
    private float amountVelocity;
    private Vector3 leanAway;     // flat world direction the top of the body tips toward
    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = visual.localScale;
    }

    private void OnEnable() => motor.Skidded += OnSkidded;
    private void OnDisable() => motor.Skidded -= OnSkidded;

    private void OnSkidded(float value) => severity = value;

    // Small fixed substeps: a stiff spring stepped with one long frame (a hitch, or an unfocused
    // editor) can overshoot wildly, so the pose would depend on frame rate. Semi-implicit Euler
    // (velocity first, then position) keeps it stable at this step size.
    private void StepSpring(float target, float dt)
    {
        const float maxStep = 1f / 240f;
        while (dt > 0f)
        {
            float h = Mathf.Min(dt, maxStep);
            amountVelocity += (stiffness * (target - amount) - damping * amountVelocity) * h;
            amount += amountVelocity * h;
            dt -= h;
        }
    }

    private void LateUpdate()
    {
        // Lean against the slide: braking pushes the feet ahead, so the top tips away from the motion.
        Vector3 slide = motor.Velocity;
        slide.y = 0f;
        if (motor.IsSkidding && slide.sqrMagnitude > 0.0001f) leanAway = -slide.normalized;

        // A damped spring (rather than a plain ease) is what gives the overshoot on release. Game time,
        // so the pose holds still during pause and hitstop with everything else.
        float target = motor.IsSkidding ? severity : 0f;
        StepSpring(target, Time.deltaTime);

        // Rotating about (up x direction) tips "up" toward that direction. Negative amounts (the
        // overshoot) tip the other way: the spring forward.
        Quaternion lean = leanAway.sqrMagnitude > 0f
            ? Quaternion.AngleAxis(maxLeanAngle * amount, Vector3.Cross(Vector3.up, leanAway))
            : Quaternion.identity;
        visual.rotation = lean * transform.rotation;

        float squash = maxSquash * Mathf.Max(0f, amount);
        visual.localScale = Vector3.Scale(baseScale, new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f));
    }
}
