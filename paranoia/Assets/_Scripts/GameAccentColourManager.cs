using System;
using UnityEngine;

public class GameAccentColourManager : MonoBehaviour
{
    public static GameAccentColourManager Instance { get; private set; }

    [SerializeField] private GameAccentColourSO currentProfile;

    public Color AccentColour => currentProfile.accentColour;

    public event Action<Color> OnAccentColourChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCharacter(GameAccentColourSO profile)
    {
        currentProfile = profile;
        OnAccentColourChanged?.Invoke(profile.accentColour);
    }
}
