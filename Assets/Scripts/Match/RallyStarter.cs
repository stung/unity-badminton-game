using UnityEngine;

/// <summary>
/// Launches the birdie from the server to start a rally. A stand-in until real serves exist,
/// so the launch needs no swing and can't fault.
/// </summary>
public class RallyStarter : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [Tooltip("The shot the launch plays. Must be a Target Depth shot.")]
    [SerializeField] private ShotData launchShot;
    [Tooltip("Launch height (m).")]
    [SerializeField] private float launchHeight = 1.3f;

    /// <summary>Where a launch from this side starts.</summary>
    public Vector3 LaunchOrigin(CourtSide side)
    {
        Vector3 origin = side.transform.position;
        origin.y = launchHeight;
        return origin;
    }

    /// <param name="server">Launches it, so they can't hit it again before the other side has.</param>
    /// <param name="targetX">Sideways landing position, normally the receiver's.</param>
    public void Launch(ShotHitter server, CourtSide side, float targetX, CourtDimensions court)
    {
        Vector3 origin = LaunchOrigin(side);
        Vector3 velocity = launchShot.GetLaunchVelocityToward(origin, side.TowardNet, targetX, court, birdie.FlightParams);
        birdie.Launch(origin, velocity, launchShot, server);
    }
}
