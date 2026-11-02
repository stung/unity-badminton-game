using UnityEngine;

public struct LandingPrediction
{
    public bool Valid;
    public Vector3 Point;
    /// <summary>Seconds from the start state until landing.</summary>
    public float Time;
    public bool HitNet;
}

/// <summary>
/// Runs the same deterministic simulation forward to find where and when the birdie lands.
/// Used by the landing marker now and by the bot later.
/// </summary>
public static class LandingPredictor
{
    public static LandingPrediction Predict(BirdieSimState state, in BirdieFlightParams p,
                                            CourtDimensions court, float maxTime = 10f)
    {
        float t = 0f;
        bool hitNet = state.HitNet;
        while (t < maxTime)
        {
            BirdieStepResult result = BirdieFlightModel.Step(ref state, p, court);
            t += p.TimeStep;
            if (result == BirdieStepResult.HitNet) hitNet = true;
            if (result == BirdieStepResult.Landed)
            {
                return new LandingPrediction { Valid = true, Point = state.Position, Time = t, HitNet = hitNet };
            }
        }
        return default;
    }
}
