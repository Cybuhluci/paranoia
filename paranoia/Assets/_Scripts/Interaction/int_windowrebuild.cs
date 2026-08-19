using UnityEngine;

public class int_windowrebuild : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "To Rebuild Barrier";
    [SerializeField] private BarrierWindow barrierscript;
    private float holdtimeperbarrierrebuild = 1f;
    private float lastInteractionTime = -1f;
    private float accumulatedHoldTime;

    // PlayerInteraction only calls OnInteract once every "requiredHoldTime" (0.25s) while the player
    // keeps holding the interact key down - so we treat consecutive pulses that arrive close together
    // as one continuous hold, and use that to time out a full second per barrier board rebuilt.
    private const float maxGapBetweenPulses = 0.4f;

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void OnInteract(GameObject interactor)
    {
        // continuously rebuild barrier until fully repaired, then stop trying to rebuild.
        // it takes 1 second to rebuild 1 barrier, so we will check if the player has held the interaction for 1 second, and if so, we will rebuild 1 barrier.
        if (barrierscript == null || !barrierscript.anyBarrierMissing)
        {
            accumulatedHoldTime = 0f;
            lastInteractionTime = -1f;
            return;
        }

        float gapSinceLastPulse = lastInteractionTime < 0f ? 0f : Time.time - lastInteractionTime;

        if (lastInteractionTime < 0f || gapSinceLastPulse > maxGapBetweenPulses)
        {
            // the player let go and pressed again (or this is the first pulse) - start a fresh hold.
            accumulatedHoldTime = 0f;
        }
        else
        {
            accumulatedHoldTime += gapSinceLastPulse;
        }

        lastInteractionTime = Time.time;

        if (accumulatedHoldTime >= holdtimeperbarrierrebuild)
        {
            barrierscript.AddBarrier();
            accumulatedHoldTime = 0f;
        }
    }

    public bool IsPressInteraction()
    {
        return false;
    }
}

