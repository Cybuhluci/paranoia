using UnityEngine;

public class int_WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt;
    [SerializeField] private GunSO gunToPickup;

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void OnInteract(GameObject interactor)
    {
        GunMain.Instance.AddGun(gunToPickup);
    }

    public bool IsPressInteraction()
    {
        return false;
    }
}