using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject zombiePrefab; // The zombie prefab to spawn
    [SerializeField] private Transform spawnPoint;

    public void ButtonUse()
    {
        GameObject zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        AI_Zombie zombai = zombie.GetComponent<AI_Zombie>();

        int health = GameDirector.Instance != null ? GameDirector.Instance.ZombieHealthOnThisRound() : 100;
        zombai.SpawnZombie(GameDirector.Instance, health);
    }
}

