using UnityEngine;
using UnityEngine.AI;

// this is the base class for all AI enemies - it handles the stuff that pretty much any enemy will need:
// the NavMeshAgent, a reference to the player, distance tracking, and basic health/death handling.
// enemy-specific behaviour (like how a zombie chases and attacks) belongs in a subclass, e.g. AI_Zombie.
public class AI_BASE : MonoBehaviour
{
    protected NavMeshAgent agent;
    protected Transform playerTransform; // cached reference to the player, so subclasses dont each need to find it themselves.
    protected float distanceToPlayer;

    [SerializeField] protected int maxHealth = 100;
    protected int currentHealth;
    protected bool isDead;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        currentHealth = maxHealth;
    }

    protected virtual void Update()
    {
        if (playerTransform != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        }
    }

    protected void SetAgentSpeed(float speed)
    {
        if (agent != null)
        {
            agent.speed = speed;
        }
    }

    protected void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    protected void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        if (agent != null)
        {
            agent.isStopped = true;
        }

        Destroy(gameObject, 3f); // give time for a death animation/ragdoll before cleanup.
    }
}
