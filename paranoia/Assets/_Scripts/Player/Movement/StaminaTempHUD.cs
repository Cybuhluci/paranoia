using UnityEngine;
using UnityEngine.UI;

public class StaminaTempHUD : MonoBehaviour
{
    Slider staminaSlider;
    [SerializeField] PlayerController playerController;

    private void Start()
    {
        staminaSlider = GetComponent<Slider>();
        staminaSlider.maxValue = playerController.GetMaxStamina();
    }

    private void Update()
    {
        staminaSlider.value = playerController.GetStamina();
    }
}
