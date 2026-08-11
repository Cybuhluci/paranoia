using UnityEngine;

public enum ZombieSpeed
{
    Shamble, // 1m/s
    Walk,    // 2m/s
    Run      // 4m/s
}

public class AI_Zombie : AI_BASE
{
    bool isAwake; // is the zombie's AI actually active? if not, it the AIagent is off completely and an idle animation plays.
    bool isActive = true; // is the zombie actively trying to kill? if not, then the zombie stands still and plays a "deactivated" animation,
                   // but the AIagent is still on, but wont try to kill until the zombie turns back on after 5 seconds of being deactivated.

    [SerializeField] private float hitRange = 1.15f;
    [SerializeField] private Transform centrepointTransform; // the point we put the collider on, so we can check if the player is close enough to be hit by zombie.
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1f; // time between attacks, so the zombie doesn't hit every single frame.

    private float nextAttackTime;

    private PlayerHealth playerHealth;

    private const float shambleSpeed = 1f;
    private const float walkSpeed = 2f;
    private const float runSpeed = 4f;

    private ZombieSpeed currentSpeed;

    int amountOfKnownAliveZombies; // how many zombies are known to be alive in the scene.
                                   // if this is 1, then the zombie will run at the player, otherwise it will shamble, walk or run depending on what it spawned as.

    int knownRound; // the round number the zombie spawned in, so we can give it a speed based on the round number. (higher rounds = faster zombies being more common)

    // zombies are melee only enemies - they will rush down the player and attack them when they are close enough.

    // zombies have 3 different speeds they can go: shamble, walk, and run.
    // shamble speed is the slowest, it is 1m/s
    // walk speed is the medium speed, it is 2m/s
    // run speed is the fastest, it is 4m/s

    // the zombie speed is given at spawn time, and will not change unless the zombie is the last zombie alive in the scene.

    // zombies will never change speeds - unless a zombie knows it is the last zombie alive, then it will run unless it is already running.

    protected override void Awake()
    {
        base.Awake();

        if (playerTransform != null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>().GetComponent<PlayerHealth>();
        }
    }

    // called by whatever spawns the zombie (e.g. a round/wave manager), sets the zombie's speed for its lifetime.
    public void SpawnZombie(ZombieSpeed speed, int amountOfKnownAliveZombies, int knownRound)
    {
        currentSpeed = speed;
        this.amountOfKnownAliveZombies = amountOfKnownAliveZombies;
        this.knownRound = knownRound;

        ApplyCurrentSpeed();

        isAwake = true;
    }

    // called whenever the zombie learns how many zombies are known to be alive - if it's the last one, it always runs.
    public void UpdateKnownAliveZombies(int amountOfKnownAliveZombies)
    {
        this.amountOfKnownAliveZombies = amountOfKnownAliveZombies;

        if (amountOfKnownAliveZombies <= 1 && currentSpeed != ZombieSpeed.Run)
        {
            currentSpeed = ZombieSpeed.Run;
            ApplyCurrentSpeed();
        }
    }

    private void ApplyCurrentSpeed()
    {
        float speed = currentSpeed switch
        {
            ZombieSpeed.Shamble => shambleSpeed,
            ZombieSpeed.Walk => walkSpeed,
            ZombieSpeed.Run => runSpeed,
            _ => shambleSpeed
        };

        SetAgentSpeed(speed);
    }

    protected override void Update()
    {
        base.Update();

        if (isDead || !isAwake || !isActive || playerTransform == null)
        {
            return;
        }

        if (distanceToPlayer <= hitRange)
        {
            StopMoving();
            TryAttackPlayer();
        }
        else
        {
            MoveTo(playerTransform.position);
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }
}

