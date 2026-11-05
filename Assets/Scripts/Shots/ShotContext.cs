/// <summary>Court depth bands, measured from the net on the hitter's own side.</summary>
public enum CourtZone { Front = 0, Mid = 1, Back = 2 }

/// <summary>The situation at the moment of contact, used to pick the actual shot.</summary>
public struct ShotContext
{
    public CourtZone Zone;
    /// <summary>Distance from the net on the hitter's side, in metres.</summary>
    public float DepthFromNet;
    /// <summary>Birdie height at contact, in metres.</summary>
    public float ContactHeight;
    /// <summary>True if the incoming shot was a smash (enables block / lift return).</summary>
    public bool ReturningSmash;
}
