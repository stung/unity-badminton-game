using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Points the EventSystem's UI input module at a player's own "UI" action map, so menus
/// are driven by the same bindings and devices as that player (DualSense or keyboard),
/// instead of Unity's built-in default actions.
/// </summary>
[RequireComponent(typeof(InputSystemUIInputModule))]
public class PlayerUIInputBinder : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string uiActionMap = "UI";

    private void Awake()
    {
        var module = GetComponent<InputSystemUIInputModule>();
        InputActionAsset actions = playerInput.actions;

        module.actionsAsset = actions;
        module.move = Reference(actions, "Navigate");
        module.submit = Reference(actions, "Submit");
        module.cancel = Reference(actions, "Cancel");
        module.point = Reference(actions, "Point");
        module.leftClick = Reference(actions, "Click");
        // Not used by our menus; cleared so the module doesn't enable leftover default actions.
        module.rightClick = null;
        module.middleClick = null;
        module.scrollWheel = null;
        module.trackedDevicePosition = null;
        module.trackedDeviceOrientation = null;

        // Lets PlayerInput restrict UI navigation to this player's paired devices (matters for split-screen).
        playerInput.uiInputModule = module;
    }

    private InputActionReference Reference(InputActionAsset actions, string actionName)
    {
        return InputActionReference.Create(actions.FindAction($"{uiActionMap}/{actionName}", throwIfNotFound: true));
    }
}
