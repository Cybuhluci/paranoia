using UnityEngine;

public class int_Button : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt;
    [SerializeField] private MonoBehaviour scriptToPlay;

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void OnInteract(GameObject interactor)
    {
        if (scriptToPlay != null)
        {
            scriptToPlay.Invoke("ButtonUse", 0f);
        }
    }

    public bool IsPressInteraction()
    {
        return true;
    } 
}