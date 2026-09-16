using UnityEngine;

[CreateAssetMenu(fileName = "ColourTheme", menuName = "Game Accent Colour")]
public class GameAccentColourSO : ScriptableObject
{
    [Header("Accent Colour Settings")]
    // Default to red
    public Color32 accentColour = new Color32(0,0,0,255); 

    // the colour text should be when the accent colour is used as a background
    public Color32 accentColourText = new Color32(0,0,0,255); 
}
