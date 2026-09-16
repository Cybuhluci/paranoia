using UnityEngine;
using UnityEngine.UI;

public class GameAccentColourChanger : MonoBehaviour
{
    private Graphic graphic;
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();

        if (selectable == null)
            graphic = GetComponent<Graphic>();

        if (graphic == null && selectable == null)
        {
            Debug.LogWarning(
                $"{name} requires either a Graphic or Selectable component."
            );
        }

        GameAccentColourManager.Instance.OnAccentColourChanged += ApplyAccent;
    }

    private void OnEnable()
    {
        ApplyAccent(GameAccentColourManager.Instance.AccentColour);
    }

    public void ApplyAccent(Color colour)
    {
        if (graphic != null)
            graphic.color = colour;

        if (selectable != null)
        {
            ColorBlock colours = selectable.colors;

            colours.highlightedColor = colour;
            colours.selectedColor = colour;

            selectable.colors = colours;
        }
    }
}