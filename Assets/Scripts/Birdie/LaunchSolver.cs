using UnityEngine;

/// <summary>
/// Finds a launch velocity that passes through a target point, using the real flight model.
/// Drag makes this impossible to solve in closed form, so we binary-search the speed.
/// Used by the debug feeder now; the bot can reuse it later.
/// </summary>
public static class LaunchSolver
{
    /// <summary>
    /// Solves for the speed (at a fixed elevation angle) whose descending path crosses target.y at
    /// target's horizontal position. Ignores the net.
    /// </summary>
    public static bool TrySolve(Vector3 from, Vector3 target, float angleDeg, in BirdieFlightParams p,
                                out Vector3 velocity, float minSpeed = 1f, float maxSpeed = 60f,
                                int iterations = 24, float maxTime = 10f)
    {
        Vector3 flat = target - from;
        flat.y = 0f;
        float targetDistance = flat.magnitude;
        Vector3 horizontal = targetDistance > 0.0001f ? flat / targetDistance : Vector3.forward;

        float elevation = angleDeg * Mathf.Deg2Rad;
        Vector3 direction = horizontal * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);

        velocity = direction * maxSpeed;
        if (DistanceAtHeight(from, velocity, target.y, p, maxTime) < targetDistance) return false;

        float lo = minSpeed, hi = maxSpeed;
        for (int i = 0; i < iterations; i++)
        {
            float mid = 0.5f * (lo + hi);
            if (DistanceAtHeight(from, direction * mid, target.y, p, maxTime) < targetDistance) lo = mid;
            else hi = mid;
        }
        velocity = direction * hi;
        return true;
    }

    /// <summary>
    /// Like TrySolve, but searches launch angles until the shot both reaches the target and clears
    /// the net by minClearance. Tries the shot's own angle and steeper first (keeps its character;
    /// low contacts near the net need a steeper arc), then flatter (extra range for far targets,
    /// e.g. cross-court from deep). If nothing works, returns the attempt that cleared the net best.
    /// </summary>
    public static Vector3 SolveClearingNet(Vector3 from, Vector3 target, float startAngleDeg, float minClearance,
                                           in BirdieFlightParams p, CourtDimensions court, float maxSpeed,
                                           float maxAngleDeg = 85f, float minAngleDeg = 5f, float angleStepDeg = 5f)
    {
        Vector3 best = Vector3.zero;
        float bestClearance = float.NegativeInfinity;
        bool haveBest = false;

        for (float angle = startAngleDeg; angle <= maxAngleDeg; angle += angleStepDeg)
        {
            if (TryAngle(angle, in p, out Vector3 v)) return v;
        }
        for (float angle = startAngleDeg - angleStepDeg; angle >= minAngleDeg; angle -= angleStepDeg)
        {
            if (TryAngle(angle, in p, out Vector3 v)) return v;
        }
        return best;

        bool TryAngle(float angle, in BirdieFlightParams flight, out Vector3 velocity)
        {
            bool inRange = TrySolve(from, target, angle, flight, out velocity, maxSpeed: maxSpeed);
            float clearance = court != null ? NetClearance(from, velocity, flight, court) : float.PositiveInfinity;
            if (inRange && clearance >= minClearance) return true;
            if (!haveBest || clearance > bestClearance)
            {
                best = velocity;
                bestClearance = clearance;
                haveBest = true;
            }
            return false;
        }
    }

    /// <summary>
    /// Height (m) above the net tape where the flight crosses the net plane. +infinity if it passes
    /// outside the posts; -infinity if it lands before reaching the net (a short shot must never
    /// count as "clearing" it). Simulated without the net so we measure the clean arc.
    /// </summary>
    public static float NetClearance(Vector3 from, Vector3 velocity, in BirdieFlightParams p, CourtDimensions court,
                                     float maxTime = 10f)
    {
        var s = new BirdieSimState { Position = from, Velocity = velocity };
        for (float t = 0f; t < maxTime; t += p.TimeStep)
        {
            Vector3 previous = s.Position;
            BirdieStepResult result = BirdieFlightModel.Step(ref s, p, null);
            if ((previous.z < 0f) != (s.Position.z < 0f))
            {
                float k = previous.z / (previous.z - s.Position.z);
                Vector3 crossing = Vector3.Lerp(previous, s.Position, k);
                return Mathf.Abs(crossing.x) <= court.HalfWidth ? crossing.y - court.NetHeight : float.PositiveInfinity;
            }
            if (result == BirdieStepResult.Landed) return float.NegativeInfinity;
        }
        return float.NegativeInfinity;
    }

    /// <summary>Horizontal distance travelled when the birdie comes down through height y, or -1 if it never does.</summary>
    private static float DistanceAtHeight(Vector3 from, Vector3 velocity, float y, in BirdieFlightParams p, float maxTime)
    {
        var s = new BirdieSimState { Position = from, Velocity = velocity };
        for (float t = 0f; t < maxTime; t += p.TimeStep)
        {
            BirdieStepResult result = BirdieFlightModel.Step(ref s, p, null);
            // Stop on the way down once at/below the target height. If the arc never got that high,
            // this triggers just after the apex and reports a short distance, so the search speeds up.
            if ((s.Velocity.y < 0f && s.Position.y <= y) || result == BirdieStepResult.Landed)
            {
                Vector3 d = s.Position - from;
                d.y = 0f;
                return d.magnitude;
            }
        }
        return -1f;
    }
}
