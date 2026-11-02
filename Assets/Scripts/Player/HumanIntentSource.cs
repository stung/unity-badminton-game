using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns a PlayerInput's actions into a CharacterIntent. Using PlayerInput (rather than a
/// global action asset) means PlayerInputManager can pair devices per player for split-screen later.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class HumanIntentSource : IntentSource
{
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string shotActionName = "Shot";
    [Tooltip("Shot played when the Shot button is pressed. Only clears exist for now.")]
    [SerializeField] private ShotData shot;

    private InputAction moveAction;
    private InputAction shotAction;

    private void Awake()
    {
        var actions = GetComponent<PlayerInput>().actions;
        moveAction = actions.FindAction(moveActionName, throwIfNotFound: true);
        shotAction = actions.FindAction(shotActionName, throwIfNotFound: true);
    }

    public override CharacterIntent ReadIntent()
    {
        return new CharacterIntent
        {
            // WASD composites can produce (1,1); clamp so diagonals aren't faster than the stick.
            Move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f),
            ShotPressed = shotAction.WasPressedThisFrame(),
            Shot = shot,
        };
    }
}
