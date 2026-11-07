using UnityEngine;

/// <summary>Who loses the point when the birdie lands. Pure rules, with no scene dependencies.</summary>
public static class PointRules
{
    /// <summary>
    /// In bounds, the side it lands on loses. Out of bounds, whoever hit it loses. A shot that hits
    /// the net drops on the hitter's side, so it needs no rule of its own.
    /// </summary>
    /// <param name="hitByNearSide">Whether the near side played the last shot.</param>
    /// <param name="court">The boundaries in play.</param>
    public static bool NearSideLoses(Vector3 landing, bool hitByNearSide, CourtDimensions court)
    {
        bool inBounds = Mathf.Abs(landing.x) <= court.HalfWidth && Mathf.Abs(landing.z) <= court.HalfLength;
        return inBounds ? landing.z < 0f : hitByNearSide;
    }
}
