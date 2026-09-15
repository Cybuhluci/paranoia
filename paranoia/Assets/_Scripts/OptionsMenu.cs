using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] TMP_Text sensitivityValueText;

    [SerializeField] Toggle toggleRunSprintToggle;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        int sensivityValue = 0;
        sensivityValue = PlayerPrefs.GetInt("Sensitivity", 1);
        sensitivitySlider.value = sensivityValue;
        sensitivityValueText.text = sensivityValue.ToString();
    }

    public void SetVolume(float volume)
    {
        // Implement volume adjustment logic here
        AudioListener.volume = volume;
    }

    public void SetSensitivity()
    {
        PlayerPrefs.SetInt("Sensitivity", (int)sensitivitySlider.value);
        sensitivityValueText.text = sensitivitySlider.value.ToString();
    }

    public void SetRunSprintToggle(bool isToggled)
    {
        PlayerPrefs.SetInt("RunSprintToggle", isToggled ? 1 : 0);
    }
}
