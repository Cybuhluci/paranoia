using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private CinemachineCamera mainMenuCamera, selectCampaignCamera;

    [SerializeField] private GameObject mainMenuUI, selectCampaignUI;

    public void SelectCampaign()
    {
        mainMenuCamera.Priority = 0;
        selectCampaignCamera.Priority = 1;
        mainMenuUI.SetActive(false);
        selectCampaignUI.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Stop play mode in the editor
#endif
        Application.Quit();
    }

    public void OpenOptions()
    {
        // Implement your options menu logic here
        Debug.Log("Options menu opened.");
    }

    public void OpenExtras()
    {
        // Implement your extras menu logic here
        Debug.Log("Extras menu opened.");
    }
}
