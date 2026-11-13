using UnityEngine;

/// <summary>
/// Sells the dash as a dive (a "superman" belly flop), in three beats:
///   1. Flight (Dashing): a small hop forward while the front legs reach ahead and the back legs
///      stretch behind.
///   2. Landing (start of DashRecovering): the body drops flat onto its belly, legs still outstretched,
///      with a little squash on impact.
///   3. Get up (rest of DashRecovering): legs fold back under, the body rises, nose first, so it's
///      standing just as control returns.
/// That keeps the lockout readable (a telegraph: the opponent can see you're down) instead of
/// feeling like dropped inputs. The body stays level rather than pitching, so the head never sinks
/// into the court.
/// Presentation only: it moves the "Visual" child and the leg pivots, never the character root, so
/// HitPoint and game logic are untouched. Runs after SkidLeanView and LegSwingView (execution order)
/// and blends on top of their poses, since they drive the same transforms.
/// </summary>
[DefaultExecutionOrder(100)]
public class DashRecoveryView : MonoBehaviour
{
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private CharacterMotor motor;
    [Tooltip("Pivot at the feet holding the body, face, legs and racket. Not the HitPoint.")]
    [SerializeField] private Transform visual;
    [Tooltip("Hip pivots of the front legs. Each hangs straight down and turns about its local X.")]
    [SerializeField] private Transform[] frontLegs = new Transform[0];
    [Tooltip("Hip pivots of the back legs.")]
    [SerializeField] private Transform[] backLegs = new Transform[0];

    [Header("Flight")]
    [Tooltip("Extra height (m) at the top of the dive's hop.")]
    [SerializeField] private float hopHeight = 0.12f;
    [Tooltip("Nose-down tilt (degrees) mid-flight, for a reaching, head-first look.")]
    [SerializeField] private float flightPitch = 10f;

    [Header("Belly")]
    [Tooltip("How far (m) the body drops to lie on its belly. About the leg length, so the belly meets the court.")]
    [SerializeField] private float bellyDrop = 0.2f;
    [Tooltip("How far (degrees) the legs stretch out from straight down. 90 = flat along the court.")]
    [SerializeField] private float legStretch = 85f;
    [Tooltip("Height squash (fraction) on landing, for a bit of impact.")]
    [Range(0f, 0.5f)] [SerializeField] private float landingSquash = 0.12f;

    [Header("Get up")]
    [Tooltip("Fraction of the recovery spent lying down before getting up.")]
    [Range(0f, 1f)] [SerializeField] private float holdFraction = 0.45f;
    [Tooltip("Nose-up tilt (degrees) halfway through getting up: the front legs push up first.")]
    [SerializeField] private float riseTilt = 12f;

    private Vector3 baseVisualPosition;

    // One frame of the pose. Zero everywhere = standing normally.
    private struct Pose
    {
        public float Height;  // m, added to the visual's base height (negative = lower)
        public float Pitch;   // degrees, positive = nose down
        public float Stretch; // 0..1 of legStretch
        public float Squash;  // fraction
    }

    private void Awake()
    {
        baseVisualPosition = visual.localPosition;
    }

    private void LateUpdate()
    {
        Pose pose = CurrentPose();
        visual.localPosition = baseVisualPosition + Vector3.up * pose.Height;
        if (pose.Stretch <= 0f && pose.Pitch == 0f && pose.Squash <= 0f) return;

        // Local X: positive tips the nose (+Z) down.
        visual.localRotation *= Quaternion.Euler(pose.Pitch, 0f, 0f);
        float s = pose.Squash;
        visual.localScale = Vector3.Scale(visual.localScale, new Vector3(1f + s * 0.5f, 1f - s, 1f + s * 0.5f));

        // A leg hangs down its local -Y. Rotating -angle about X swings the foot forward (+Z), +angle back.
        // Blend from whatever LegSwingView set, so the legs flow out of the trot rather than snapping.
        Quaternion front = Quaternion.Euler(-legStretch, 0f, 0f);
        Quaternion back = Quaternion.Euler(legStretch, 0f, 0f);
        foreach (Transform leg in frontLegs) leg.localRotation = Quaternion.Slerp(leg.localRotation, front, pose.Stretch);
        foreach (Transform leg in backLegs) leg.localRotation = Quaternion.Slerp(leg.localRotation, back, pose.Stretch);
    }

    private Pose CurrentPose()
    {
        float t = stateMachine.StateTime;
        switch (stateMachine.CurrentState)
        {
            case CharacterStateMachine.State.Dashing:
            {
                float p = Mathf.Clamp01(t / Mathf.Max(motor.DashDuration, 0.0001f));
                return new Pose
                {
                    // Sink toward the belly height while a sine hop rides on top: up first, then down
                    // onto the belly right as the dash ends.
                    Height = -bellyDrop * p * p + hopHeight * Mathf.Sin(p * Mathf.PI),
                    Pitch = flightPitch * Mathf.Sin(p * Mathf.PI),
                    // Legs reach out fast, fully stretched by halfway through the flight.
                    Stretch = Smooth(Mathf.Clamp01(p * 2f)),
                };
            }

            case CharacterStateMachine.State.DashRecovering:
            {
                float u = Mathf.Clamp01(t / Mathf.Max(stateMachine.DashRecovery, 0.0001f));
                if (u <= holdFraction)
                {
                    // On the belly. Squash right at touchdown, easing off over the hold.
                    float hold = holdFraction > 0f ? u / holdFraction : 1f;
                    return new Pose { Height = -bellyDrop, Stretch = 1f, Squash = landingSquash * (1f - hold) };
                }

                float r = Smooth((u - holdFraction) / Mathf.Max(1f - holdFraction, 0.0001f));
                return new Pose
                {
                    Height = -bellyDrop * (1f - r),
                    Pitch = -riseTilt * Mathf.Sin(r * Mathf.PI),
                    // Legs tuck back under a bit ahead of the body rising, so it looks like they push it up.
                    Stretch = 1f - Smooth(Mathf.Clamp01(r * 1.5f)),
                };
            }

            default:
                return default;
        }
    }

    private static float Smooth(float x) => x * x * (3f - 2f * x);
}
