using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    // this script handles every gamestate in the zombies mode. it is a singleton, so it can be accessed from anywhere.
    // such things it handles are: game start, game end, round progression, zombie spawning, game pausing, and more.

    [SerializeField] private PlayerController playerController;
    [SerializeField] private GunMain gunMain;
    [SerializeField] private GameObject gameOverUI; // the "you died"/game over screen shown to the player.
    [SerializeField] private GameObject playerHUDUI; // the player's HUD, which is hidden when the player is downed or dead.
    [SerializeField] private CinemachineBrain cinemachineBrain; // used to force an instant cut instead of a blend when switching to the game over camera.
    [SerializeField] private float gameOverCameraDelay = 5f; // delay between the player going down and the camera cutting to the game over camera.
    [SerializeField] private AudioSource gameOverSound; // sound played when the player is downed and the game over camera is activated.
    [SerializeField] private AudioSource roundStartSound, roundEndSound, specialroundStartSound, specialroundEndSound;

    [SerializeField] private TMP_Text roundCounterText, zombiesLeftText; // the text that shows the current round number to the player.

    [SerializeField] private CinemachineCamera gameOverCamera; // reference to the game over camera
    // sounds played when a round starts or ends, and when a special round starts or ends.

    private const int gameOverCameraPriority = 115; // always the highest priority, so it takes over from every other camera.

    [Header("Game Values")]
    public int currentRound { get; private set; } // the current round the player is on
    public int zombiesLeft { get; private set; } // zombies left in the current round
    public int zombiesAlive { get; private set; } // zombies currently alive in the current round
    private int maxZombiesAtOnce = 32; // the maximum amount of zombies that can be alive at once.

    [SerializeField] private GameObject zombiePrefab; // the zombie prefab to spawn at each ZombieSpawnPoint.
    [SerializeField] private int specialRoundInterval = 5; // every Nth round is a "special" round (e.g. plays special round sounds).
    [SerializeField] private float roundEndDelay = 8f; // delay between a round ending and the next one starting.

    private ZombieSpawnPoint[] zombieSpawnPoints; // all the spawn points in the scene, used to spawn zombies at random locations.
    private Coroutine spawnRoutine;
    private bool isRoundActive;

    public int ZombiesOnThisRound()
    {
        if (currentRound <= 4)
        {
            switch (currentRound)
            {
                case 1:
                    return 6;
                case 2:
                    return 9;
                case 3:
                    return 12;
                case 4:
                    return 18;
                default:
                    return 0; // should never ever happen
            }
        }
        else
        {
            int zomboes = 24 + (int)(0.5 * ((currentRound - 4) * (currentRound - 4))); // round 1 = 24
            return zomboes;
        }
    }

    public int ZombieHealthOnThisRound()
    {
        if (currentRound <= 9)
        {
            int health = 100 * (currentRound - 9);
            return health;
        }
        else
        {
            int health = 900 * (int)Mathf.Pow(1.1f, currentRound - 9);
            return health;
        }
    }

    public int ZombieSpawnDelayOnThisRound() // in seconds
    {
        if (currentRound <= 9)
        {
            float delay = 7f - (0.25f * (currentRound - 1)); // round 1 = 7 seconds, round 2 = 6.75 seconds, round 3 = 6.5 seconds, etc. round 9 = 5 seconds
            return Mathf.RoundToInt(delay);
        }
        else
        {
            float delay = 5f * Mathf.Pow(0.95f, currentRound - 9); // round 10 = 2 seconds, round 11 = 1.9 seconds, round 12 = 1.8 seconds, etc. 
            if (delay < 0.5f)
            {
                delay = 0.5f; // minimum spawn delay is 0.5 seconds
            }
            return Mathf.RoundToInt(delay);
        }
    }

    private void Awake()
    {
        Instance = this;

        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindAnyObjectByType<CinemachineBrain>();
        }

        zombieSpawnPoints = FindObjectsByType<ZombieSpawnPoint>(FindObjectsSortMode.None);

        StartGame();
    }

    private void Update()
    {
        roundCounterText.text = $"{currentRound}";
        zombiesLeftText.text = $"Zombies Left: {zombiesLeft}";
    }

    #region Game State Management

    public void StartGame()
    {
        // set values to their defaults, and start the first round.
        currentRound = 0;
        zombiesLeft = 0;

        StartRound();
    }

    public void EndGame()
    {
        // called once the player has been fully downed with no self-revive available, or has bled out from a failed self-revive.
        // strips the player of their weapons, locks them into a prone/immobile state, and (after a delay) cuts to the game over screen.

        if (playerController != null)
        {
            playerController.SetDowned(true);
        }

        if (gunMain != null)
        {
            gunMain.DisableWeapons();
        }

        if (gameOverSound != null)
        {
            gameOverSound.Play();
        }

        StartCoroutine(GameOverCameraRoutine());
    }

    private IEnumerator GameOverCameraRoutine()
    {
        yield return new WaitForSeconds(gameOverCameraDelay);

        if (cinemachineBrain != null)
        {
            // force an instant cut instead of a blend, so the camera doesn't smoothly fly/clip through walls.
            CinemachineBlendDefinition cutBlend = cinemachineBrain.DefaultBlend;
            cutBlend.Style = CinemachineBlendDefinition.Styles.Cut;
            cinemachineBrain.DefaultBlend = cutBlend;
        }

        if (gameOverCamera != null)
        {
            PrioritySettings priority = gameOverCamera.Priority;
            priority.Value = gameOverCameraPriority;
            gameOverCamera.Priority = priority;
        }

        if (gameOverUI != null)
        {
            playerHUDUI.SetActive(false);
            gameOverUI.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion

    #region Round Management

    public void StartRound()
    {
        currentRound++;
        zombiesLeft = ZombiesOnThisRound();
        isRoundActive = true;

        bool isSpecialRound = specialRoundInterval > 0 && currentRound % specialRoundInterval == 0;

        if (isSpecialRound && specialroundStartSound != null)
        {
            specialroundStartSound.Play();
        }
        else if (roundStartSound != null)
        {
            roundStartSound.Play();
        }

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(SpawnZombiesRoutine());
    }

    public void EndRound()
    {
        isRoundActive = false;

        bool isSpecialRound = specialRoundInterval > 0 && currentRound % specialRoundInterval == 0;

        if (isSpecialRound && specialroundEndSound != null)
        {
            specialroundEndSound.Play();
        }
        else if (roundEndSound != null)
        {
            roundEndSound.Play();
        }

        StartCoroutine(StartNextRoundAfterDelay());
    }

    private IEnumerator StartNextRoundAfterDelay()
    {
        yield return new WaitForSeconds(roundEndDelay);
        StartRound();
    }

    // called by each AI_Zombie once it dies - tracks alive count and ends the round once everything has been killed.
    public void OnZombieDeath()
    {
        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
        zombiesLeft--;
        zombiesAlive--;

        if (isRoundActive && zombiesLeft <= 0 && zombiesAlive <= 0)
        {
            EndRound();
        }
    }

    #endregion

    #region Zombie Spawning

    private IEnumerator SpawnZombiesRoutine()
    {
        while (zombiesLeft > 0)
        {
            if (zombiesAlive < maxZombiesAtOnce && zombieSpawnPoints != null && zombieSpawnPoints.Length > 0 && zombiePrefab != null)
            {
                SpawnZombies();
                yield return new WaitForSeconds(ZombieSpawnDelayOnThisRound());
            }
            else
            {
                yield return null;
            }
        }

        spawnRoutine = null;
    }

    public void SpawnZombies()
    {
        if (zombieSpawnPoints == null || zombieSpawnPoints.Length == 0 || zombiePrefab == null || zombiesLeft <= 0)
        {
            return;
        }

        ZombieSpawnPoint spawnPoint = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Length)];
        spawnPoint.SpawnZombie(zombiePrefab, this);

        //zombiesLeft--;
        zombiesAlive++;
    }

    #endregion

    #region Game Pausing

    public void PauseGame()
    {
        // pause the game
    }

    public void ResumeGame()
    {
        // resume the game
    }

    #endregion
}
