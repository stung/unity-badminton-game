using UnityEngine;

/// <summary>
/// Which half of the court a character owns. Converts side-relative intents to world space
/// and keeps the character inside its half.
/// </summary>
public class CourtSide : MonoBehaviour
{
    public enum Side { Near = -1, Far = 1 }

    [SerializeField] private Side side = Side.Near;
    [SerializeField] private CourtDimensions court;
    [Tooltip("How close to the net plane the character may get (keeps them from touching the net).")]
    [SerializeField] private float netMargin = 0.4f;
    [Tooltip("How far outside the side/baselines the character may run (players often chase out of court).")]
    [SerializeField] private float outOfBoundsAllowance = 1.0f;

    public CourtDimensions Court => court;

    /// <summary>-1 for the near half (z &lt; 0), +1 for the far half (z &gt; 0).</summary>
    public float Sign => (float)side;

    /// <summary>World direction pointing at the net from this side.</summary>
    public Vector3 TowardNet => new Vector3(0f, 0f, -Sign);

    /// <summary>World direction of the character's right when facing the net.</summary>
    public Vector3 Right => new Vector3(-Sign, 0f, 0f);

    public Vector3 ToWorld(Vector2 sideRelative)
    {
        return Right * sideRelative.x + TowardNet * sideRelative.y;
    }

    public Vector3 ClampToHalf(Vector3 position)
    {
        float maxX = court.HalfWidth + outOfBoundsAllowance;
        float maxDepth = court.HalfLength + outOfBoundsAllowance;

        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        // Depth measured as distance from the net on our side (always positive).
        float depth = Mathf.Clamp(position.z * Sign, netMargin, maxDepth);
        position.z = depth * Sign;
        return position;
    }
}
