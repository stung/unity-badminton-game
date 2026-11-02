using UnityEngine;

/// <summary>Position/velocity of a simulated birdie. Plain data so it can be copied for prediction.</summary>
public struct BirdieSimState
{
    public Vector3 Position;
    public Vector3 Velocity;
    /// <summary>Set once the birdie has hit the net, after which it just falls.</summary>
    public bool HitNet;
}

public enum BirdieStepResult { Flying, HitNet, Landed }

/// <summary>
/// Deterministic birdie physics: gravity plus quadratic drag, fixed time step, no Unity physics.
/// The live birdie and LandingPredictor both call Step with the same inputs, so predictions match
/// the real flight exactly (same code, same order of float operations).
/// </summary>
public static class BirdieFlightModel
{
    // Where the birdie is placed after hitting the net, so it falls on the hitter's side, not inside the net.
    private const float NetBounceOffset = 0.05f;

    /// <param name="court">Used for the net. Pass null to ignore the net (e.g. for feed solving).</param>
    public static BirdieStepResult Step(ref BirdieSimState s, in BirdieFlightParams p, CourtDimensions court)
    {
        float dt = p.TimeStep;
        Vector3 v = s.Velocity;

        // Quadratic drag opposes velocity and grows with speed², which is why a shuttle leaves the
        // racket fast and then "stalls" and drops almost vertically. That steep fall is the iconic clear shape.
        Vector3 acceleration = Vector3.down * p.Gravity - p.DragCoefficient * v.magnitude * v;

        // Semi-implicit Euler: update velocity first, then position with the new velocity. It's
        // stable for drag at this step size and costs the same as explicit Euler.
        v += acceleration * dt;
        Vector3 previous = s.Position;
        Vector3 next = previous + v * dt;

        if (court != null && !s.HitNet && (previous.z < 0f) != (next.z < 0f))
        {
            // Find where the step crosses the net plane (z = 0) and test it against the net's extent.
            float t = previous.z / (previous.z - next.z);
            Vector3 crossing = Vector3.Lerp(previous, next, t);
            if (crossing.y < court.NetHeight && Mathf.Abs(crossing.x) <= court.HalfWidth)
            {
                // Simple net fault: kill velocity and drop on the side it came from.
                s.Position = new Vector3(crossing.x, crossing.y, Mathf.Sign(previous.z) * NetBounceOffset);
                s.Velocity = Vector3.zero;
                s.HitNet = true;
                return BirdieStepResult.HitNet;
            }
        }

        if (next.y <= 0f)
        {
            next.y = 0f;
            s.Position = next;
            s.Velocity = Vector3.zero;
            return BirdieStepResult.Landed;
        }

        s.Position = next;
        s.Velocity = v;
        return BirdieStepResult.Flying;
    }
}
