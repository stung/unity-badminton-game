using UnityEngine;

/// <summary>
/// Checks whether the birdie is within reach of the character's hit point and, if so, launches it
/// with the requested shot. Called by the state machine during a swing's active window.
/// </summary>
[RequireComponent(typeof(CourtSide))]
public class ShotHitter : MonoBehaviour
{
    [Tooltip("Where the racket meets the birdie (child transform above/in front of the character).")]
    [SerializeField] private Transform hitPoint;
    [Tooltip("Max distance (m) between the birdie and the hit point for contact.")]
    [SerializeField] private float reachRadius = 1.0f;
    [SerializeField] private Birdie birdie;

    private CourtSide courtSide;

    private void Awake()
    {
        courtSide = GetComponent<CourtSide>();
    }

    /// <returns>True if the birdie was hit.</returns>
    public bool TryHit(ShotData shot, float aimX)
    {
        if (birdie == null || !birdie.IsInFlight) return false;
        if ((birdie.Position - hitPoint.position).sqrMagnitude > reachRadius * reachRadius) return false;

        Vector3 from = birdie.Position;
        birdie.Launch(from, shot.GetLaunchVelocity(from, courtSide.TowardNet, aimX, birdie.FlightParams));
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitPoint == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawWireSphere(hitPoint.position, reachRadius);
    }
}
