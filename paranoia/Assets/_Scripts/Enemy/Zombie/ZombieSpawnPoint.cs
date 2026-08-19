using UnityEngine;

public class ZombieSpawnPoint : MonoBehaviour
{
    public void SpawnZombie(GameObject zombieprefab, GameDirector director)
    {
        GameObject zombie = Instantiate(zombieprefab, transform.position, transform.rotation);
        AI_Zombie zombai = zombie.GetComponent<AI_Zombie>();
        zombai.SpawnZombie(director, director.ZombieHealthOnThisRound());
    }
}
