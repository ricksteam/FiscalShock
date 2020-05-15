using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script for controlling the buttons on the win screen.
/// </summary>
public class WinScreen : MonoBehaviour {
    private GameObject loadingScreen;
    private LoadingScreen loadScript;

    private void Start() {
        Settings.forceUnlockCursorState();
        loadingScreen = GameObject.FindGameObjectWithTag("Loading Screen");
        loadScript = loadingScreen.GetComponent<LoadingScreen>();
    }

    /// <summary>
    /// Closes the game and quits.
    /// </summary>
    public void QuitClick() {
        Debug.Log("Quit by win game.");
        StateManager.playerWon = false;
        Settings.quitToDesktop();
    }

    /// <summary>
    /// Bravely puts the player back into the hub to continue earning.
    /// </summary>
    public void GoBack() {
        GameObject.FindGameObjectWithTag("Spawn Point").GetComponent<SpawnPoint>().resetToHubDefaults();
        StateManager.playerWon = false;
        loadScript.startLoadingScreen("Hub");
    }
}
