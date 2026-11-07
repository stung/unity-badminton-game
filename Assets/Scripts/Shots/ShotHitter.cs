using System;
using UnityEngine;

/// <summary>
/// During a swing's active window: checks whether the birdie is within reach, works out the
/// contact situation, asks the ShotTable which shot that category becomes, and launches it.
/// </summary>
[RequireComponent(typeof(CourtSide))]
public class ShotHitter : MonoBehaviour
{
    [SerializeField] private ShotTable shotTable;
    [SerializeField] private Birdie birdie;

    [Header("Reach")]
    [Tooltip("Centre of the reach area, in front of the character. Only its horizontal position is used.")]
    [SerializeField] private Transform hitPoint;
    [Tooltip("Horizontal reach (m) around the hit point.")]
    [SerializeField] private float reachRadius = 1.0f;
    [Tooltip("Lowest contact height (m). Low enough for lifts and net drops off the floor.")]
    [SerializeField] private float minReachHeight = 0.1f;
    [Tooltip("Highest contact height (m): an overhead at full stretch.")]
    [SerializeField] private float maxReachHeight = 3.1f;

    private CourtSide courtSide;
    private CharacterMotor motor;

    /// <summary>Raised on every successful hit, for feedback/debug UI.</summary>
    public event Action<ShotData, ShotContext> ShotPlayed;

    private void Awake()
    {
        courtSide = GetComponent<CourtSide>();
        motor = GetComponent<CharacterMotor>();
    }

    // The reach heights are measured from the character's feet, so a jump lifts the whole reach area:
    // the birdie can be met earlier, but low shots go out of reach while in the air.
    private float Lift => motor != null ? motor.HeightAboveGround : 0f;

    /// <param name="aimX">-1..1 sideways aim at contact, relative to this character facing the net.</param>
    /// <returns>True if the birdie was hit.</returns>
    public bool TryHit(ShotCategory category, float aimX)
    {
        if (birdie == null || !birdie.IsInFlight || !InReach(birdie.Position)) return false;
        // No double hits: you can't play the birdie again until the opponent has.
        if (ReferenceEquals(birdie.LastHitter, this)) return false;

        Vector3 contact = birdie.Position;
        ShotContext context = BuildContext(contact);
        ShotData shot = shotTable != null ? shotTable.Select(category, context) : null;
        if (shot == null)
        {
            Debug.LogWarning($"[ShotHitter] No ShotTable rule for {category} in {context.Zone} court " +
                             $"at {context.ContactHeight:F2} m (smash return: {context.ReturningSmash}).");
            return false;
        }

        Vector3 velocity = shot.GetLaunchVelocity(contact, courtSide.TowardNet, aimX, courtSide.Court, birdie.FlightParams);
        birdie.Launch(contact, velocity, shot, this);
        ShotPlayed?.Invoke(shot, context);
        return true;
    }

    private bool InReach(Vector3 point)
    {
        // A vertical cylinder rather than a sphere: contact can be anywhere from the floor to
        // full overhead stretch, which low shots (lifts, net drops, blocks) need.
        Vector3 offset = point - hitPoint.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= reachRadius * reachRadius
               && point.y >= minReachHeight + Lift && point.y <= maxReachHeight + Lift;
    }

    private ShotContext BuildContext(Vector3 contact)
    {
        // Distance from the net on our own side: towardNet points at the net, so it's the negative projection.
        float depth = Mathf.Max(0f, -Vector3.Dot(contact, courtSide.TowardNet));
        return new ShotContext
        {
            Zone = shotTable.ZoneFor(depth),
            DepthFromNet = depth,
            ContactHeight = contact.y,
            ReturningSmash = birdie.IncomingSmash,
        };
    }

    private void OnDrawGizmosSelected()
    {
        if (hitPoint == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Vector3 c = hitPoint.position;
        Vector3 bottom = new Vector3(c.x, minReachHeight + Lift, c.z);
        Vector3 top = new Vector3(c.x, maxReachHeight + Lift, c.z);
        Gizmos.DrawWireSphere(bottom, reachRadius);
        Gizmos.DrawWireSphere(top, reachRadius);
        Gizmos.DrawLine(bottom + Vector3.left * reachRadius, top + Vector3.left * reachRadius);
        Gizmos.DrawLine(bottom + Vector3.right * reachRadius, top + Vector3.right * reachRadius);
        Gizmos.DrawLine(bottom + Vector3.forward * reachRadius, top + Vector3.forward * reachRadius);
        Gizmos.DrawLine(bottom + Vector3.back * reachRadius, top + Vector3.back * reachRadius);
    }
}
