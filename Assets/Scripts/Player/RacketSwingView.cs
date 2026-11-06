using UnityEngine;

/// <summary>
/// Animates the racket from the character's state: a simple overhead swing for every shot (for now).
/// Procedural rather than an Animator clip so it reads the same SwingTiming as the hit logic and can
/// never drift out of sync with the real contact window. Presentation only: nothing reads from this.
/// Put it on the shoulder pivot the racket hangs from. Rotation is around the pivot's local X axis:
/// 0° = racket straight up, positive = tipping forward toward the net, negative = back behind the head.
/// </summary>
public class RacketSwingView : MonoBehaviour
{
    [SerializeField] private CharacterStateMachine stateMachine;

    [Header("Pose angles (degrees around local X; 0 = straight up, + = forward)")]
    [Tooltip("Ready position between shots: racket up and slightly in front.")]
    [SerializeField] private float readyAngle = 35f;
    [Tooltip("Fully cocked behind the head at the end of the windup.")]
    [SerializeField] private float cockedAngle = -120f;
    [Tooltip("End of the swing: racket down in front after follow-through.")]
    [SerializeField] private float followThroughAngle = 125f;

    [Header("Shape")]
    [Tooltip("Shapes the main swing across the active window (0..1 in, 0..1 out). " +
             "Ease-in-out makes it whip through the top, where contact happens.")]
    [SerializeField] private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Small sideways roll (degrees) at mid-swing so the arc doesn't look perfectly flat.")]
    [SerializeField] private float swingRoll = 15f;

    private Quaternion baseRotation;

    private void Awake()
    {
        baseRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float angle = readyAngle;
        float roll = 0f;
        SwingTiming timing = stateMachine.SwingTiming;
        float t = stateMachine.StateTime;

        switch (stateMachine.CurrentState)
        {
            case CharacterStateMachine.State.Swinging:
                if (t < timing.Windup)
                {
                    // Windup is very short (50 ms by default): snap back to cocked.
                    angle = Mathf.Lerp(readyAngle, cockedAngle, Ease(t / Mathf.Max(timing.Windup, 0.0001f)));
                }
                else
                {
                    // Sweep cocked -> follow-through across the active window, so the racket passes
                    // overhead (the contact zone) while contact is actually possible.
                    float u = Mathf.Clamp01((t - timing.Windup) / Mathf.Max(timing.ActiveWindow, 0.0001f));
                    float k = swingCurve.Evaluate(u);
                    angle = Mathf.LerpUnclamped(cockedAngle, followThroughAngle, k);
                    roll = Mathf.Sin(k * Mathf.PI) * swingRoll;
                }
                break;

            case CharacterStateMachine.State.Recovering:
                // Ease back from the follow-through to the ready pose over the recovery time.
                float r = Mathf.Clamp01(t / Mathf.Max(timing.Recovery, 0.0001f));
                angle = Mathf.Lerp(followThroughAngle, readyAngle, Ease(r));
                break;
        }

        transform.localRotation = baseRotation * Quaternion.Euler(angle, 0f, roll);
    }

    private static float Ease(float x) => x * x * (3f - 2f * x);
}
