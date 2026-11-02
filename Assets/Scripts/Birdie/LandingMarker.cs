using UnityEngine;

/// <summary>
/// Shows a ring where the birdie will land, using the prediction made at launch.
/// Hidden while the birdie isn't in flight.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class LandingMarker : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [SerializeField] private bool showMarker = true;
    [SerializeField] private float groundOffset = 0.015f;

    private Renderer markerRenderer;

    private void Awake()
    {
        markerRenderer = GetComponent<Renderer>();
        markerRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (birdie == null) return;
        birdie.Launched += HandleLaunched;
        birdie.Landed += HandleLanded;
    }

    private void OnDisable()
    {
        if (birdie == null) return;
        birdie.Launched -= HandleLaunched;
        birdie.Landed -= HandleLanded;
    }

    private void HandleLaunched(LandingPrediction prediction)
    {
        markerRenderer.enabled = showMarker && prediction.Valid;
        if (prediction.Valid)
        {
            transform.position = new Vector3(prediction.Point.x, groundOffset, prediction.Point.z);
        }
    }

    private void HandleLanded(Vector3 point)
    {
        markerRenderer.enabled = false;
    }
}
