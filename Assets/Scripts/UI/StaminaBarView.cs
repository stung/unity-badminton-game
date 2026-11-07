using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draws a character's stamina as a bar that fades green to yellow to red as it empties.
/// Reads from CharacterStamina only; game logic never references this.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StaminaBarView : MonoBehaviour
{
    [SerializeField] private CharacterStamina stamina;
    [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 0.25f);
    [SerializeField] private Color midColor = new Color(0.95f, 0.8f, 0.15f);
    [SerializeField] private Color emptyColor = new Color(0.85f, 0.15f, 0.12f);

    private VisualElement fill;

    private void Update()
    {
        if (stamina == null) return;
        // Looked up lazily: the document's tree only exists once the UIDocument has been enabled.
        if (fill == null)
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            fill = root?.Q<VisualElement>("stamina-fill");
            if (fill == null) return;
        }

        float amount = stamina.Normalized;
        fill.style.width = Length.Percent(amount * 100f);
        // Two blends meeting at half stamina, so the bar passes through a true yellow.
        fill.style.backgroundColor = amount >= 0.5f
            ? Color.Lerp(midColor, fullColor, (amount - 0.5f) * 2f)
            : Color.Lerp(emptyColor, midColor, amount * 2f);
    }
}
