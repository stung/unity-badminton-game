using UnityEngine;

/// <summary>Tuning for MovementRules. Plain data so tests can build it without a scene.</summary>
public struct MovementTuning
{
    public float MaxSpeed;
    /// <summary>Speed (fraction of MaxSpeed) the quick burst reaches before the slower build takes over.</summary>
    public float BurstFraction;
    /// <summary>m/s² from standstill up to the burst speed. High, so the first step feels instant.</summary>
    public float BurstAcceleration;
    /// <summary>m/s² from the burst speed up to top speed. Low, so top speed takes a moment to reach.</summary>
    public float BuildAcceleration;
    /// <summary>m/s² when the stick is released, or when slowing to a lower target speed.</summary>
    public float Deceleration;
    /// <summary>Turns up to this many degrees from the current heading carry speed over for free.</summary>
    public float FreeTurnAngle;
    /// <summary>Degrees per second a free turn can swing the heading. Stops a quick stick sweep dodging the skid.</summary>
    public float FreeTurnRate;
    /// <summary>m/s² at which wrong-way momentum bleeds off in a sharp turn. Lower = longer, costlier skids.</summary>
    public float SkidDeceleration;
}

/// <summary>
/// Pure ground-movement math: one call per frame turns (current velocity, stick, dt) into a new velocity.
/// No Unity objects, so Edit Mode tests can step it directly.
///
/// Velocity is split into the part along the stick direction and the "wrong-way" part (sideways, or
/// backwards). The along part speeds up in two phases (a quick burst, then a slower build to top
/// speed). The wrong-way part has to be braked off first: that's the turnaround penalty (a skid).
/// Braking at a fixed rate means the faster you were going, the longer the skid lasts, and the more
/// of your speed points the wrong way (a sharper turn), the more there is to brake.
/// </summary>
public static class MovementRules
{
    /// <param name="move">Desired world direction scaled by stick push (magnitude 0..1).</param>
    /// <param name="speedMultiplier">Lets states (e.g. swinging) cap the speed lower.</param>
    /// <param name="skidding">Whether a skid was already underway, so letting go of the stick can't cut it short.</param>
    /// <param name="wrongWaySpeed">Speed (m/s) still heading somewhere other than the stick after this step.</param>
    public static Vector3 Step(Vector3 velocity, Vector3 move, float speedMultiplier, in MovementTuning t, float dt,
                               bool skidding, out float wrongWaySpeed)
    {
        velocity.y = 0f;
        move.y = 0f;
        wrongWaySpeed = 0f;
        float push = Mathf.Min(1f, move.magnitude);
        if (push < 0.0001f)
        {
            // Mid-skid, letting go keeps skidding: otherwise flicking through neutral would swap the
            // skid for the snappier plain stop and dodge the penalty.
            if (skidding)
            {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, t.SkidDeceleration * dt);
                wrongWaySpeed = velocity.magnitude;
                return velocity;
            }
            // Otherwise letting go of the stick is a plain stop, not a skid.
            return Vector3.MoveTowards(velocity, Vector3.zero, t.Deceleration * dt);
        }

        Vector3 direction = move / move.magnitude;

        // Gentle turns: swing the heading toward the stick without losing speed, but only at a limited
        // rate. Without the rate cap, sweeping the stick round in a few small steps would turn 180°
        // "gently" and skip the skid entirely.
        // The heading carries all the speed through the turn, so nothing counts as wrong-way.
        float target = t.MaxSpeed * speedMultiplier * push;
        if (velocity.sqrMagnitude > 0.0001f && Vector3.Angle(velocity, direction) <= t.FreeTurnAngle)
        {
            float maxRadians = t.FreeTurnRate * Mathf.Deg2Rad * dt;
            Vector3 heading = Vector3.RotateTowards(velocity.normalized, direction, maxRadians, 0f);
            return heading * AccelerateAlong(velocity.magnitude, target, t, dt);
        }

        float along = Vector3.Dot(velocity, direction);
        Vector3 across = velocity - direction * along;

        across = Vector3.MoveTowards(across, Vector3.zero, t.SkidDeceleration * dt);
        along = AccelerateAlong(along, target, t, dt);

        wrongWaySpeed = Mathf.Sqrt(across.sqrMagnitude + Mathf.Min(0f, along) * Mathf.Min(0f, along));
        return direction * along + across;
    }

    /// <summary>Same as the full Step, for callers that don't care about the skid.</summary>
    public static Vector3 Step(Vector3 velocity, Vector3 move, float speedMultiplier, in MovementTuning t, float dt)
        => Step(velocity, move, speedMultiplier, t, dt, false, out _);

    /// <summary>Speed along the stick direction after dt. Negative means still moving the opposite way.</summary>
    public static float AccelerateAlong(float speed, float target, in MovementTuning t, float dt)
    {
        if (speed >= target) return Mathf.MoveTowards(speed, target, t.Deceleration * dt);

        // Each phase below spends only the time it needs and hands the rest of the frame to the next.
        // Without the split, a long frame would overshoot a phase at the wrong rate, so timings would
        // change with frame rate.

        // Still moving backwards: skid to a stop before building speed the new way.
        if (speed < 0f)
        {
            float timeToStop = -speed / Mathf.Max(t.SkidDeceleration, 0.0001f);
            if (timeToStop >= dt) return speed + t.SkidDeceleration * dt;
            speed = 0f;
            dt -= timeToStop;
        }

        // Quick burst up to the burst speed.
        float burstSpeed = Mathf.Min(target, t.MaxSpeed * t.BurstFraction);
        if (speed < burstSpeed)
        {
            float timeToBurst = (burstSpeed - speed) / Mathf.Max(t.BurstAcceleration, 0.0001f);
            if (timeToBurst >= dt) return speed + t.BurstAcceleration * dt;
            speed = burstSpeed;
            dt -= timeToBurst;
        }

        // Slower build to the target speed.
        return Mathf.MoveTowards(speed, target, t.BuildAcceleration * dt);
    }
}
