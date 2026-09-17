using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    public InputAction pauseAction;
    public InputAction resumeAction;
    public InputAction tabSwitchAction;

    [SerializeField] GameObject PauseScreenFull, PauseFront, OptionsScreen;
    [SerializeField] Image pauseTabButton, optionsTabButton;

    Color tabImageColour;

    bool isPaused = false;
    int currentTab = 0; // 0 = PauseFront, 1 = OptionsScreen

    private void Awake()
    {
        pauseAction = playerInput.actions["Player/Pause"]; // Key Press - this is one the Player ActionMap
        resumeAction = playerInput.actions["UI/Pause"]; // Key Press - this is one the UI ActionMap
        tabSwitchAction = playerInput.actions["UI/TabSwitch"]; // 1D axis - this is one the UI ActionMap
    }

    private void Start()
    {
        // Unity does not guarantee Awake() execution order between different scripts, so GameAccentColourManager's
        // own Awake() (which sets Instance) might not have run yet if we subscribed here instead. Every script's
        // Awake() is guaranteed to finish before any Start() runs, so subscribing here ensures Instance is always set.
        UpdateAccents();
    }

    void UpdateAccents()
    {
        GameAccentColourManager.Instance.OnAccentColourChanged += UpdateTabButtonColours;
        tabImageColour = GameAccentColourManager.Instance.AccentColour;
    }

    void UpdateTabButtonColours(Color colour)
    {
        tabImageColour = colour;
        if (!isPaused) return;

        if (currentTab == 0)
        {
            pauseTabButton.color = tabImageColour;
            optionsTabButton.color = Color.gray;
        }
        else
        {
            pauseTabButton.color = Color.gray;
            optionsTabButton.color = tabImageColour;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pauseAction.triggered || resumeAction.triggered)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        if (isPaused)
        {
            float tabSwitchValue = tabSwitchAction.ReadValue<float>();
            if (tabSwitchValue > 0.5f && currentTab == 0)
            {
                OpenOptionsTab();
            }
            else if (tabSwitchValue < -0.5f && currentTab == 1)
            {
                OpenPauseMainTab();
            }
        }

        if (isPaused)
        {
            // if on keyboard, then make the cursor visible, otherwise hide it for controller
            if (playerInput.currentControlScheme == "Keyboard&Mouse")
            {
                Cursor.visible = true;
            }
            else
            {
                Cursor.visible = false;
            }
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        PauseScreenFull.SetActive(true);
        isPaused = true;
        playerInput.SwitchCurrentActionMap("UI"); // Switch to UI ActionMap
        OpenPauseMainTab();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        PauseScreenFull.SetActive(false);
        isPaused = false;
        playerInput.SwitchCurrentActionMap("Player"); // Switch back to Player ActionMap
    }

    public void OpenPauseMainTab()
    {
        currentTab = 0;
        OptionsScreen.SetActive(false);
        PauseFront.SetActive(true);
        pauseTabButton.color = tabImageColour;
        optionsTabButton.color = Color.gray;
    }

    public void OpenOptionsTab()
    {
        currentTab = 1;
        PauseFront.SetActive(false);
        OptionsScreen.SetActive(true);
        pauseTabButton.color = Color.gray;
        optionsTabButton.color = tabImageColour;
    }

    public void QuitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitToDesktop()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}