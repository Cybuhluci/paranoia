using System.Collections;
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
    [SerializeField] private float gameOverCameraDelay = 2f; // delay between the player going down and the camera cutting to the game over camera.

    private const int gameOverCameraPriority = 115; // always the highest priority, so it takes over from every other camera.

    private void Awake()
    {
        Instance = this;

        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindAnyObjectByType<CinemachineBrain>();
        }
    }

    private void Update()
    {

    }

    #region Game State Management
    public CinemachineCamera gameOverCamera; // reference to the game over camera

    public void StartGame()
    {
        // start the game
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
        // start a new round
    }

    public void EndRound()
    {
        // end the current round
    }

    #endregion

    #region Zombie Spawning

    public void SpawnZombies()
    {
        // spawn zombies
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
