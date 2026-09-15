using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    // this script is on the playercameralookattarget, so using transform.forward will be the direction the player is looking at,

    [SerializeField] private TMP_Text interactionText;
    // interaction text is split into 4 parts: prompt ("press" or "hold"), action key ("F"), action ("to buy door"), and cost ("cost: 100")
    // a full example of the text would be: "Press F to buy door (cost: 100)"

    private string actionKeyColour = "<color=#E7D764>"; // yellow

    private float interactionRange = 2.5f; // how far the player can interact with objects in a sphere radius
    [SerializeField] private LayerMask interactableLayer; // the layer that interactable objects are on

    [SerializeField] private float interactionSphereRadius = 0.5f; // radius of the sphere used to detect interactables.
    [SerializeField] private string interactActionKeyDisplayName = "F"; // shown in the interaction text, should match the actual bound key.

    private InputAction interactAction;

    private IInteractable currentInteractable;
    private GameObject currentInteractableObject;

    public bool isHoldInteracting = false;
    private float holdInteractionTime; // how long the hold interaction has been held, in seconds.
    private float requiredHoldTime = 0.25f; // how long the player needs to hold the interact key for a hold interaction.

    private bool interactionLatched = false; // true once an interaction has fired, prevents re-firing until the key is released.

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        interactAction = playerInput.actions["Interact"];
    }

    private void Update()
    {
        DetectInteractable();
        HandleInteractionInput();
        UpdateInteractionText();
    }

    private void DetectInteractable()
    {
        if (Physics.SphereCast(transform.position, interactionSphereRadius, transform.forward, out RaycastHit hit, interactionRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractableObject != hit.collider.gameObject)
                {
                    // switched to a new interactable, reset any hold progress from the previous one.
                    ResetHoldInteraction();
                }

                currentInteractable = interactable;
                currentInteractableObject = hit.collider.gameObject;
                return;
            }
        }

        // nothing valid found this frame.
        if (currentInteractable != null)
        {
            ResetHoldInteraction();
        }

        currentInteractable = null;
        currentInteractableObject = null;
    }

    private void HandleInteractionInput()
    {
        if (currentInteractable == null)
        {
            return;
        }

        // once the key is released, allow interactions to fire again.
        if (interactionLatched && !interactAction.IsPressed())
        {
            interactionLatched = false;
        }

        if (interactionLatched)
        {
            return;
        }

        if (currentInteractable.IsPressInteraction())
        {
            if (interactAction.WasPressedThisFrame())
            {
                currentInteractable.OnInteract(gameObject);
                interactionLatched = true;
            }
        }
        else
        {
            if (interactAction.IsPressed())
            {
                isHoldInteracting = true;
                holdInteractionTime += Time.deltaTime;

                if (holdInteractionTime >= requiredHoldTime)
                {
                    currentInteractable.OnInteract(gameObject);
                    ResetHoldInteraction();
                    interactionLatched = true;
                }
            }
            else
            {
                ResetHoldInteraction();
            }
        }
    }

    private void ResetHoldInteraction()
    {
        isHoldInteracting = false;
        holdInteractionTime = 0f;
    }

    private void UpdateInteractionText()
    {
        if (interactionText == null)
        {
            return;
        }

        if (currentInteractable == null)
        {
            interactionText.text = string.Empty;
            interactionText.gameObject.SetActive(false);
            return;
        }

        interactionText.gameObject.SetActive(true);

        string prompt = currentInteractable.IsPressInteraction() ? "Press" : "Hold";
        string keyDisplay = $"{actionKeyColour}{interactActionKeyDisplayName}</color>";
        string action = currentInteractable.GetInteractionPrompt();

        interactionText.text = $"{prompt} {keyDisplay} {action}";
    }
}

public interface IInteractable
{
    // Called when player interacts (presses the interact key)
    void OnInteract(GameObject interactor);
    // Short prompt to display (e.g. "Open Door" / "Pick Lock")
    string GetInteractionPrompt();
    // Whether this object is a press or hold interaction
    bool IsPressInteraction();
}