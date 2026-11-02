using UnityEngine;

/// <summary>
/// A flat disc on the ground directly under the birdie. Depth is hard to read in 3D,
/// and a drop shadow is the classic way to show where a ball is over the court.
/// </summary>
public class BirdieShadow : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [Tooltip("Lift above the ground to avoid z-fighting with the court.")]
    [SerializeField] private float groundOffset = 0.01f;
    [Tooltip("Shadow shrinks to this fraction of its size as the birdie climbs to fadeHeight.")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float fadeHeight = 8f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (birdie == null) return;
        // Follow the rendered transform (not the raw sim position) so the shadow doesn't jitter against it.
        Vector3 p = birdie.transform.position;
        transform.position = new Vector3(p.x, groundOffset, p.z);
        float t = Mathf.Clamp01(p.y / fadeHeight);
        transform.localScale = baseScale * Mathf.Lerp(1f, minScale, t);
    }
}
