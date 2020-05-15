﻿using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script to control the main menu of the game.
/// </summary>
public class MainMenu : MonoBehaviour {
    private void Start() {
        Settings.forceUnlockCursorState();
        Settings.loadSettings();
    }

    /// <summary>
    /// Starts the game by loading the player into the hub.
    /// </summary>
    public void PlayClick() {
        Debug.Log("Starting game...");
        if (Settings.values.sawStoryTutorial) {
            SceneManager.LoadScene("Hub");
        } else {
            SceneManager.LoadScene("Story");
        }
    }

    /// <summary>
    /// Quits the game and closes the application.
    /// </summary>
    public void QuitClick() {
        Debug.Log("Quitting from main menu.");
        Settings.quitToDesktop();
    }
}
