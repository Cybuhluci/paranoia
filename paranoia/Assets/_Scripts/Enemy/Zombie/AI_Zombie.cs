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

    private GameDirector gameDirector;
    [SerializeField] private float hitRange = 1.15f;
    [SerializeField] private Transform centrepointTransform; // the point we put the collider on, so we can check if the player is close enough to be hit by zombie.
    [SerializeField] private int attackDamage = 2;
    [SerializeField] private float attackCooldown = 1f; // time between attacks, so the zombie doesn't hit every single frame.

    private float nextAttackTime;

    private PlayerHealth playerHealth;

    private const float shambleSpeed = 0.5f;
    private const float walkSpeed = 1f;
    private const float runSpeed = 2f;

    private ZombieSpeed currentSpeed;

    #region Barriers
    // zombies outside of the playable bounds cannot pathfind to the player directly - instead they must
    // find the nearest barrier window, walk up to its "startpoint" (outside), break down its barriers,
    // then "climb through" (phasing collision-free from startpoint to endpoint) before resuming the hunt.
    private enum ZombieBarrierState
    {
        None,           // not dealing with a barrier - hunting normally.
        MovingToBarrier,
        BreakingBarrier,
        ClimbingThrough
    }

    [SerializeField] private float barrierBreakRange = 1.2f; // how close the zombie needs to be to the barrier's startpoint to begin breaking it.
    [SerializeField] private float barrierBreakInterval = 1.25f; // seconds between each barrier removal "hit".
    [SerializeField] private float climbThroughDuration = 2f; // seconds it takes to phase through a broken barrier window.
    [SerializeField] private float pathCheckInterval = 1f; // how often (in seconds) to re-check if the player is reachable, to avoid running this every frame.

    private ZombieBarrierState barrierState = ZombieBarrierState.None;
    private BarrierWindow targetBarrier;
    private float nextPathCheckTime;
    private float nextBarrierBreakTime;
    private float climbElapsed;
    private Vector3 climbStartPosition;
    private Vector3 climbEndPosition;
    #endregion

    int knownZombiesAlive; // how many zombies are known to be alive in the scene.
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
    public void SpawnZombie(GameDirector director, int health)
    {
        gameDirector = director;
        // set the zombie's speed based on the round number and a random chance.
        knownRound = director.currentRound;
        currentHealth = health;

        currentSpeed = knownRound switch
        {
            <= 3 => Random.value < 0.95f ? ZombieSpeed.Shamble : ZombieSpeed.Walk, // rounds 1-3 are 95% shamble, 5% walk.
            <= 7 => Random.value < 0.5f ? ZombieSpeed.Shamble : ZombieSpeed.Walk, // rounds 4-7 are 50% shamble, 50% walk.
            <= 15 => Random.value < 0.5f ? ZombieSpeed.Walk : ZombieSpeed.Run, // rounds 8-15 are 50% walk, 50% run.
            _ => ZombieSpeed.Run // rounds 16+ are always run speed.
        };

        ApplyCurrentSpeed();

        isAwake = true;
    }

    // called whenever the zombie learns how many zombies are known to be alive - if it's the last one, it always runs.
    public void UpdateKnownAliveZombies(int ZombiesAlive)
    {
        this.knownZombiesAlive = ZombiesAlive;

        if (ZombiesAlive <= 1 && currentSpeed != ZombieSpeed.Run && knownRound > 3)
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

        if (isDead || !isAwake || !isActive || playerTransform == null || playerHealth.isdeadalready == true)
        {
            return;
        }

        UpdateKnownAliveZombies(gameDirector.zombiesAlive);

        // currently mid-barrier-sequence (moving to it, breaking it, or climbing through it) - handle that instead of normal hunting.
        if (barrierState != ZombieBarrierState.None)
        {
            HandleBarrierState();
            return;
        }

        if (distanceToPlayer <= hitRange)
        {
            StopMoving();
            TryAttackPlayer();
            return;
        }

        // periodically check if the player is actually reachable - if not, we're outside the map and need to find a barrier to break through.
        if (Time.time >= nextPathCheckTime)
        {
            nextPathCheckTime = Time.time + pathCheckInterval;

            if (!HasPathTo(playerTransform.position))
            {
                BarrierWindow nearestBarrier = FindNearestUnbrokenBarrier();

                if (nearestBarrier != null)
                {
                    targetBarrier = nearestBarrier;
                    barrierState = ZombieBarrierState.MovingToBarrier;
                    HandleBarrierState();
                    return;
                }
            }
        }

        MoveTo(playerTransform.position);
    }

    private BarrierWindow FindNearestUnbrokenBarrier()
    {
        BarrierWindow[] barrierWindows = FindObjectsByType<BarrierWindow>(FindObjectsSortMode.None);

        BarrierWindow nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (BarrierWindow barrierWindow in barrierWindows)
        {
            if (barrierWindow == null || barrierWindow.startpoint == null || barrierWindow.endpoint == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, barrierWindow.startpoint.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = barrierWindow;
            }
        }

        return nearest;
    }

    private void HandleBarrierState()
    {
        if (targetBarrier == null)
        {
            barrierState = ZombieBarrierState.None;
            return;
        }

        switch (barrierState)
        {
            case ZombieBarrierState.MovingToBarrier:
                MoveTo(targetBarrier.startpoint.position);

                if (Vector3.Distance(transform.position, targetBarrier.startpoint.position) <= barrierBreakRange)
                {
                    StopMoving();
                    barrierState = ZombieBarrierState.BreakingBarrier;
                    nextBarrierBreakTime = 0f;
                }
                break;

            case ZombieBarrierState.BreakingBarrier:
                if (targetBarrier.allBarriersGone)
                {
                    StartClimbingThrough();
                    break;
                }

                if (Time.time >= nextBarrierBreakTime)
                {
                    nextBarrierBreakTime = Time.time + barrierBreakInterval;
                    targetBarrier.RemoveBarrier();
                }
                break;

            case ZombieBarrierState.ClimbingThrough:
                climbElapsed += Time.deltaTime;
                float climbProgress = Mathf.Clamp01(climbElapsed / climbThroughDuration);
                transform.position = Vector3.Lerp(climbStartPosition, climbEndPosition, climbProgress);

                if (climbProgress >= 1f)
                {
                    FinishClimbingThrough();
                }
                break;
        }
    }

    private void StartClimbingThrough()
    {
        barrierState = ZombieBarrierState.ClimbingThrough;
        climbElapsed = 0f;
        climbStartPosition = targetBarrier.startpoint.position;
        climbEndPosition = targetBarrier.endpoint.position;

        // phase through the barrier collision-free while "climbing", and let the NavMeshAgent catch up afterwards.
        SetCollisionEnabled(false);

        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    private void FinishClimbingThrough()
    {
        transform.position = climbEndPosition;

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(climbEndPosition);
        }

        SetCollisionEnabled(true);

        targetBarrier = null;
        barrierState = ZombieBarrierState.None;
        nextPathCheckTime = Time.time; // immediately re-check the path to the player now that we're inside.
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

    protected override void Die()
    {
        if (!isDead && gameDirector != null)
        {
            gameDirector.OnZombieDeath();
        }

        base.Die();
    }
}


