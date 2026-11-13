using UnityEngine;

/// <summary>
/// Tracks stamina: dashes and jumps spend it, standing still and walking restore it, running slowly
/// drains it. It's measured from the character's actual speed rather than stick input, so it works
/// the same for a bot. It doesn't affect what the character can do yet (see NOTES.md).
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterStateMachine))]
public class CharacterStamina : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;

    [Header("Costs")]
    [SerializeField] private float dashCost = 15f;
    [SerializeField] private float jumpCost = 15f;

    [Header("Rates (stamina per second)")]
    [SerializeField] private float stillRegen = 25f;
    [SerializeField] private float walkRegen = 5f;
    [Tooltip("Lost per second while running.")]
    [SerializeField] private float runDrain = 3f;
    [Tooltip("Restored per second while stuck in dash recovery. Slower than standing still: getting up " +
             "off the floor isn't resting.")]
    [SerializeField] private float dashRecoveryRegen = 5f;

    [Header("Speed bands (fraction of top speed)")]
    [Tooltip("Below this counts as standing still. Above zero so the end of a slowdown already counts.")]
    [SerializeField] private float stillBelow = 0.05f;
    [Tooltip("At or above this counts as running. Between the two counts as walking.")]
    [SerializeField] private float runAtOrAbove = 0.7f;

    private CharacterMotor motor;
    private CharacterStateMachine stateMachine;

    public float Current { get; private set; }
    public float Max => maxStamina;
    public float DashRecoveryRegen => dashRecoveryRegen;
    /// <summary>Current stamina as 0..1.</summary>
    public float Normalized => maxStamina > 0f ? Current / maxStamina : 0f;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        stateMachine = GetComponent<CharacterStateMachine>();
        Current = maxStamina;
    }

    private void OnEnable()
    {
        stateMachine.Dashed += OnDashed;
        stateMachine.Jumped += OnJumped;
    }

    private void OnDisable()
    {
        stateMachine.Dashed -= OnDashed;
        stateMachine.Jumped -= OnJumped;
    }

    public void Refill() => Current = maxStamina;

    private void OnDashed(Vector3 direction) => Change(-dashCost);
    private void OnJumped() => Change(-jumpCost);

    private void Update()
    {
        // Dashes and jumps already paid up front, so neither restores nor drains while it plays out.
        var state = stateMachine.CurrentState;
        if (motor.IsAirborne || state == CharacterStateMachine.State.Dashing) return;

        // Dash recovery has its own slow rate: the dog is nearly still, so the standing-still regen
        // would otherwise hand back most of the dash cost while it lies on the floor.
        float rate = state == CharacterStateMachine.State.DashRecovering ? dashRecoveryRegen : RatePerSecond();

        // Time.deltaTime is 0 during pause and hitstop, so stamina holds still then.
        Change(rate * Time.deltaTime);
    }

    private float RatePerSecond()
    {
        Vector3 velocity = motor.Velocity;
        velocity.y = 0f;
        float speedFraction = motor.MaxSpeed > 0f ? velocity.magnitude / motor.MaxSpeed : 0f;

        if (speedFraction < stillBelow) return stillRegen;
        return speedFraction >= runAtOrAbove ? -runDrain : walkRegen;
    }

    private void Change(float amount)
    {
        Current = Mathf.Clamp(Current + amount, 0f, maxStamina);
    }
}
