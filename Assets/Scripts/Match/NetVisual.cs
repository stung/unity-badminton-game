using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sizes the net's greybox parts from the court asset, so net height is one number. Runs in Edit
/// mode too, so the scene shows the real net. Presentation only: the flight model reads
/// CourtDimensions directly and nothing reads from these transforms.
/// </summary>
[ExecuteAlways]
public class NetVisual : MonoBehaviour
{
    [SerializeField] private CourtDimensions court;
    [SerializeField] private Transform netMesh;
    [SerializeField] private Transform netTape;
    [SerializeField] private Transform postLeft;
    [SerializeField] private Transform postRight;
    [Tooltip("Net mesh height as a fraction of net height. Real net: 0.76 m deep on a 1.55 m net.")]
    [Range(0.1f, 1f)] [SerializeField] private float meshHeightFraction = 0.49f;

    private float appliedHeight = float.NaN;

    private void OnEnable()
    {
        Apply();
#if UNITY_EDITOR
        // Edit-mode Update only runs when the scene changes, and editing the court asset doesn't
        // count, so poll to pick up net height edits right away.
        EditorApplication.update += Refresh;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= Refresh;
#endif
    }

    private void Refresh()
    {
        if (court != null && !Mathf.Approximately(court.NetHeight, appliedHeight)) Apply();
    }

    private void Apply()
    {
        if (court == null) return;
        float height = court.NetHeight;
        appliedHeight = height;

        if (netMesh != null)
        {
            float meshHeight = height * meshHeightFraction;
            SetHeight(netMesh, height - meshHeight * 0.5f, meshHeight);
        }
        if (netTape != null)
        {
            // A cube's height equals its Y scale. Keep the tape's thickness and sit it at the net top.
            SetHeight(netTape, height - netTape.localScale.y * 0.5f, netTape.localScale.y);
        }
        // A Unity cylinder is 2 units tall at scale 1, so its Y scale is half its height.
        if (postLeft != null) SetHeight(postLeft, height * 0.5f, height * 0.5f);
        if (postRight != null) SetHeight(postRight, height * 0.5f, height * 0.5f);
    }

    private static void SetHeight(Transform part, float y, float scaleY)
    {
        Vector3 position = part.localPosition;
        position.y = y;
        part.localPosition = position;
        Vector3 scale = part.localScale;
        scale.y = scaleY;
        part.localScale = scale;
    }
}
