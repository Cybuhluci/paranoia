using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    float maxHealth = 100f; // 3 hits to down from normal zombies
    float maxArmour = 50f; // 3 hits to down from normal zombies

    public float currentHealth { get; private set; }
    public TMP_Text _CurrentHealth; // this is shown in the inspector for debugging purposes

    public float currentArmour { get; private set; }
    public TMP_Text _CurrentArmour; // this is shown in the inspector for debugging purposes

    public bool hasSelfRevive = false; // if the player has a self-revive item, they can revive themselves once when downed. // Temp false for now.
    public bool isdeadalready = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentArmour = maxArmour;
    }

    public void TakeDamage(int amount)
    {
        // if player has armour, we still do hp damage until 2 hp remains.
        // then we start taking armour damage (if we have armour). once armour is gone, we take hp damage again.
        // then once all hp is gone, the player is downed.

        if (currentHealth > 2) // if we have more than 2 hp, we take hp damage.
        {
            RemoveHealth(amount);
        }
        else if (currentArmour > 0) // if we have 2 or less hp and armour, we take armour damage.
        {
            RemoveArmour(amount);
        }
        else // if we have no armour and 2 or less hp, we take hp damage.
        {
            RemoveHealth(amount);
        }
    }

    public void AddHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void RemoveHealth(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void AddArmour(int amount)
    {
        currentArmour += amount;
        if (currentArmour > maxArmour)
        {
            currentArmour = maxArmour;
        }
    }

    public void RemoveArmour(int amount)
    {
        currentArmour -= amount;
        if (currentArmour < 0)
        {
            currentArmour = 0;
        }
    }

    void Die()
    {
        // Handle player death (e.g., trigger game over, respawn, etc.)
        Debug.Log("Player is downed!");

        // when entering the downed state, we should check if the player has a self-revive item.
        // If they do, we can allow them to revive themselves once, else it just ends the game.
        if (hasSelfRevive)
        {
            // player enters downed state (prone height), and is unable to move, but can shoot and revive themselves.
            // the player should have their guns stripped of their person, and be given the starting pistol to use while downed (which changes based on the current round).
            // PlayerController should handle the downed state and disable movement, GunMain/GunRuntime handles shooting, and PlayerHealth handles the self-revive.
            // Allow the player to revive themselves once (using "Interact" playerinputaction). After reviving, set hasSelfRevive to false.
        }
        else
        {
            // triggers the Game Over method in the GameDirector script, which handles the game over state, UI and dolly camera.
            // player should have all guns removed from their person, and should go down to prone height, unable to move or shoot.
            // The game over camera priority should be set to 115 (so it will always be highest priority), and the game over UI should be displayed.
            Debug.Log("Game Over!");

            if (isdeadalready) return; // prevent multiple calls to EndGame
            isdeadalready = true;
        }
    }

    private void Update()
    {
        _CurrentHealth.text = currentHealth.ToString(); // update the inspector value for debugging
        _CurrentArmour.text = currentArmour.ToString(); // update the inspector value for debugging
    }
}
