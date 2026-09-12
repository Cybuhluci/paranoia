using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectCampaign : MonoBehaviour
{
    public void ContinueGame()
    {
        // Implement your logic to continue the game here
        Debug.Log("Continue Game selected.");
        SceneManager.LoadScene("paranoia"); 
    }

    public void NewGame()
    {
        // Implement your logic to start a new game here
        Debug.Log("New Game started.");
    }

    public void SelectChapter()
    {
        // Implement your logic to select a chapter here
        Debug.Log("Chapter selection opened.");
    }

    public void BackToMainMenu()
    {
        // Implement your logic to go back to the main menu here
        Debug.Log("Returning to Main Menu.");
    }
}
