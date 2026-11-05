using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug-only stand-in for an opponent: launches the birdie from the far side so it comes down
/// through the player's hit point. Remove or disable once the bot exists.
/// </summary>
public class DebugFeeder : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [Tooltip("The player's hit point. The feed is aimed to come down through it.")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 feedOrigin = new Vector3(0f, 2.2f, 5.5f);
    [Tooltip("Higher angle = steeper, slower-falling feed that's easier to time.")]
    [SerializeField] private float feedAngleDeg = 55f;
    [Tooltip("Speed cap for the solver (m/s). Realistic drag needs ~90 m/s to feed the back court.")]
    [SerializeField] private float maxFeedSpeed = 150f;
    [Tooltip("Random horizontal offset (m) added to the target so you have to move a little.")]
    [SerializeField] private Vector2 randomOffset = new Vector2(1.0f, 1.0f);
    [Tooltip("Default bindings (R / Triangle) are added at runtime if this is left empty.")]
    [SerializeField] private InputAction feedAction = new InputAction("Feed", InputActionType.Button);

    private void Awake()
    {
        if (feedAction.bindings.Count == 0)
        {
            feedAction.AddBinding("<Keyboard>/r");
            feedAction.AddBinding("<Gamepad>/buttonNorth");
        }
    }

    private void OnEnable() => feedAction.Enable();
    private void OnDisable() => feedAction.Disable();

    private void Update()
    {
        if (feedAction.WasPressedThisFrame()) Feed();
    }

    public void Feed()
    {
        if (birdie == null || target == null) return;

        Vector3 aim = target.position + new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x), 0f,
            Random.Range(-randomOffset.y, randomOffset.y));

        if (LaunchSolver.TrySolve(feedOrigin, aim, feedAngleDeg, birdie.FlightParams, out Vector3 velocity,
                                    maxSpeed: maxFeedSpeed))
        {
            birdie.Launch(feedOrigin, velocity);
        }
        else
        {
            Debug.LogWarning("[DebugFeeder] Target out of range for the current flight params.");
        }
    }
}
