using UnityEngine;

/// <summary>
/// Per-player camera behind their baseline that loosely follows the player.
/// Owns its own viewport rect so split-screen is just two of these with different rects.
/// Game logic never reads from this.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Normalized screen rect. (0,0,1,1) = full screen; (0,0,0.5,1) = left half.")]
    [SerializeField] private Rect viewport = new Rect(0f, 0f, 1f, 1f);
    [Tooltip("Camera position when the target is at the centre of its half.")]
    [SerializeField] private Vector3 basePosition = new Vector3(0f, 7f, -13f);
    [Tooltip("Point the camera looks at when the target is at the centre of its half.")]
    [SerializeField] private Vector3 baseLookAt = new Vector3(0f, 0f, 1.5f);
    [Tooltip("How much of the target's movement the camera follows, per axis (0 = fixed, 1 = locked).")]
    [SerializeField] private Vector3 followAmount = new Vector3(0.5f, 0f, 0.3f);
    [SerializeField] private float smoothTime = 0.2f;

    private Vector3 smoothVelocity;
    private Vector3 referencePoint;

    private void Awake()
    {
        GetComponent<Camera>().rect = viewport;
        // Measure follow offsets from where the target starts so the base pose is the default view.
        referencePoint = target != null ? target.position : Vector3.zero;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 delta = Vector3.Scale(target.position - referencePoint, followAmount);
        Vector3 desired = basePosition + delta;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref smoothVelocity, smoothTime);
        transform.LookAt(baseLookAt + delta);
    }
}
