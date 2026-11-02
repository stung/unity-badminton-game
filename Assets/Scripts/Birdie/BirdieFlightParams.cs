using System;
using UnityEngine;

/// <summary>Tunable constants for the birdie flight simulation.</summary>
[Serializable]
public struct BirdieFlightParams
{
    [Tooltip("m/s² downward.")]
    [SerializeField] private float gravity;
    [Tooltip("Quadratic drag k in a = -k|v|v. Terminal velocity = sqrt(g/k). " +
             "A real shuttle is ~0.21 (≈6.8 m/s terminal); lower is floatier and travels further.")]
    [SerializeField] private float dragCoefficient;
    [Tooltip("Fixed simulation step in seconds. Smaller = more accurate. Changing it changes trajectories slightly.")]
    [SerializeField] private float timeStep;

    public BirdieFlightParams(float gravity, float dragCoefficient, float timeStep)
    {
        this.gravity = gravity;
        this.dragCoefficient = dragCoefficient;
        this.timeStep = timeStep;
    }

    // k = 0.12 is floatier than a real shuttle (terminal ≈ 9 m/s) so clears reach the back of the
    // court at sane launch speeds and give the player time to read them.
    public static BirdieFlightParams Default => new BirdieFlightParams(9.81f, 0.12f, 1f / 120f);

    public float Gravity => gravity;
    public float DragCoefficient => dragCoefficient;
    public float TimeStep => Mathf.Max(0.001f, timeStep);
}
