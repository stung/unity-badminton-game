using UnityEngine;

/// <summary>
/// Swing timing shared by every shot. It can't be per-shot: the shot is only decided at contact,
/// after the swing has already started.
/// </summary>
[CreateAssetMenu(fileName = "SwingTiming", menuName = "Badminton/Swing Timing")]
public class SwingTiming : ScriptableObject
{
    [Tooltip("Seconds from button press until the racket can make contact.")]
    [SerializeField] private float windup = 0.05f;
    [Tooltip("Seconds contact is possible. Longer = more forgiving timing.")]
    [SerializeField] private float activeWindow = 0.25f;
    [Tooltip("Seconds after the active window before the character can act again.")]
    [SerializeField] private float recovery = 0.25f;
    [Tooltip("Movement speed multiplier while swinging/recovering.")]
    [SerializeField] private float swingMoveMultiplier = 0.3f;

    public float Windup => windup;
    public float ActiveWindow => activeWindow;
    public float Recovery => recovery;
    public float SwingMoveMultiplier => swingMoveMultiplier;
}
