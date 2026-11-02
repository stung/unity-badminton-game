using UnityEngine;

/// <summary>
/// Court geometry in metres (1 unit = 1 m). The origin is the centre of the net:
/// the near half is z &lt; 0, the far half is z &gt; 0, and the net runs along the x axis.
/// </summary>
[CreateAssetMenu(fileName = "CourtDimensions", menuName = "Badminton/Court Dimensions")]
public class CourtDimensions : ScriptableObject
{
    [Tooltip("Baseline to baseline (real court: 13.40 m).")]
    [SerializeField] private float length = 13.4f;
    [Tooltip("Sideline to sideline, doubles (real court: 6.10 m).")]
    [SerializeField] private float width = 6.1f;
    [Tooltip("Height of the net top (real: 1.55 m at the posts, 1.524 m at centre).")]
    [SerializeField] private float netHeight = 1.55f;

    public float Length => length;
    public float Width => width;
    public float NetHeight => netHeight;
    public float HalfLength => length * 0.5f;
    public float HalfWidth => width * 0.5f;
}
