using UnityEngine;

/// <summary>
/// Kicks up dust at the feet during a skid: a puff when it starts, sized by severity, then a light
/// trail while sliding. A common way to sell a hard stop. Presentation only.
/// The particle look (colour, size, lifetime) is set on the ParticleSystem itself; this only says
/// when and how much to emit.
/// </summary>
public class SkidDustView : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private ParticleSystem dust;

    [Tooltip("Particles in the starting puff at full severity.")]
    [SerializeField] private int puffCount = 14;
    [Tooltip("Particles per second while sliding at top speed.")]
    [SerializeField] private float trailRate = 30f;
    [Tooltip("How much of the slide speed the dust inherits (it's kicked up in the slide direction).")]
    [SerializeField] private float inheritVelocity = 0.4f;

    private float trailBudget;

    private void OnEnable() => motor.Skidded += OnSkidded;
    private void OnDisable() => motor.Skidded -= OnSkidded;

    private void OnSkidded(float severity)
    {
        Emit(Mathf.Max(1, Mathf.RoundToInt(puffCount * severity)));
    }

    private void Update()
    {
        if (!motor.IsSkidding || motor.IsAirborne) return;
        float speedFraction = motor.Velocity.magnitude / Mathf.Max(motor.MaxSpeed, 0.0001f);
        // Accumulate fractional particles so low rates still emit steadily instead of never.
        trailBudget += trailRate * speedFraction * Time.deltaTime;
        int count = Mathf.FloorToInt(trailBudget);
        if (count <= 0) return;
        trailBudget -= count;
        Emit(count);
    }

    private void Emit(int count)
    {
        Vector3 slide = motor.Velocity;
        slide.y = 0f;
        var p = new ParticleSystem.EmitParams
        {
            // Emit at the feet, wherever the character is, and push the dust along the slide.
            position = new Vector3(transform.position.x, transform.position.y - motor.HeightAboveGround + 0.03f, transform.position.z),
            velocity = slide * inheritVelocity,
            applyShapeToPosition = true,
        };
        dust.Emit(p, count);
    }
}
