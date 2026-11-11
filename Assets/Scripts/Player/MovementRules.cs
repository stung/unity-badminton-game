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
    /// <summary>m/s² at which sideways or backwards momentum is shed when the stick points a new way.</summary>
    public float TurnDeceleration;
}

/// <summary>
/// Pure ground-movement math: one call per frame turns (current velocity, stick, dt) into a new velocity.
/// No Unity objects, so Edit Mode tests can step it directly.
///
/// Velocity is split into the part along the stick direction and the part across it. The along part
/// speeds up in two phases (a quick burst, then a slower build to top speed); the across part is
/// shed separately. Keeping them apart lets speed buildup and turning be tuned independently.
/// </summary>
public static class MovementRules
{
    /// <param name="move">Desired world direction scaled by stick push (magnitude 0..1).</param>
    /// <param name="speedMultiplier">Lets states (e.g. swinging) cap the speed lower.</param>
    public static Vector3 Step(Vector3 velocity, Vector3 move, float speedMultiplier, in MovementTuning t, float dt)
    {
        velocity.y = 0f;
        move.y = 0f;
        float push = Mathf.Min(1f, move.magnitude);
        if (push < 0.0001f)
        {
            return Vector3.MoveTowards(velocity, Vector3.zero, t.Deceleration * dt);
        }

        Vector3 direction = move / move.magnitude;
        float along = Vector3.Dot(velocity, direction);
        Vector3 across = velocity - direction * along;

        across = Vector3.MoveTowards(across, Vector3.zero, t.TurnDeceleration * dt);
        along = AccelerateAlong(along, t.MaxSpeed * speedMultiplier * push, t, dt);
        return direction * along + across;
    }

    /// <summary>Speed along the stick direction after dt. Negative means still moving the opposite way.</summary>
    public static float AccelerateAlong(float speed, float target, in MovementTuning t, float dt)
    {
        if (speed >= target) return Mathf.MoveTowards(speed, target, t.Deceleration * dt);

        // Below the burst speed (including still moving backwards) use the fast rate. If this frame
        // crosses the burst speed, spend only the time needed to reach it at the fast rate and the rest
        // at the slow rate. Without the split, a long frame would overshoot using the fast rate, so
        // the time to top speed would change with frame rate.
        float burstSpeed = Mathf.Min(target, t.MaxSpeed * t.BurstFraction);
        if (speed < burstSpeed)
        {
            float timeToBurst = (burstSpeed - speed) / Mathf.Max(t.BurstAcceleration, 0.0001f);
            if (timeToBurst >= dt) return speed + t.BurstAcceleration * dt;
            speed = burstSpeed;
            dt -= timeToBurst;
        }
        return Mathf.MoveTowards(speed, target, t.BuildAcceleration * dt);
    }
}
