using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject zombiePrefab; // The zombie prefab to spawn
    [SerializeField] private Transform spawnPoint;

    public void ButtonUse()
    {
        GameObject zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        AI_Zombie zombai = zombie.GetComponent<AI_Zombie>();
        zombai.SpawnZombie(ZombieSpeed.Shamble, 2, 1);
    }
}
