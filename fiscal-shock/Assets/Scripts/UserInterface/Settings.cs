using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles control of global game settings and cursor locking/unlocking.
/// </summary>
public static class Settings {
    /// <summary>
    /// Backing value field that can be serialized to JSON
    /// </summary>
    public static SettingsValues values = new SettingsValues();

    /// <summary>
    /// Reference to the current owner of the cursor state mutex
    /// </summary>
    public static MonoBehaviour cursorStateMutexOwner { get; private set; }

    /// <summary>
    /// Passthrough accesses of Settings.volume to the backing data class
    /// </summary>
    public static float volume {
        get => values.volume;
        set => values.volume = value;
    }

    /// <summary>
    /// Passthrough accesses of Settings.mouseSensitivity to the backing
    /// data class
    /// </summary>
    public static float mouseSensitivity {
        get => values.mouseSensitivity;
        set => values.mouseSensitivity = value;
    }

    // ------------- keybinds ---------------
    public static string pauseKey => values.pauseKey;
    public static string interactKey => values.interactKey;
    public static string weaponOneKey => values.weaponOneKey;
    public static string weaponTwoKey => values.weaponTwoKey;
    public static string hidePauseMenuKey => values.hidePauseMenuKey;

    /// <summary>
    /// Filename for the settings file.
    /// <para>Linux: $XDG_CONFIG_HOME/unity3d/Download Moar RAM/fiscal-shock/</para>
    /// <para>Windows: C:\Users\%USERNAME%\AppData\LocalLow\Download Moar RAM\fiscal-shock\</para>
    /// </summary>
    private static readonly string settingsFilename = Application.persistentDataPath + "/settings.json";

    /// <summary>
    /// Serializes settings data class to JSON and write to file.
    /// </summary>
    public static void saveSettings() {
        Utils.saveToJson(values, settingsFilename);
    }

    /// <summary>
    /// Load settings data from JSON.
    /// </summary>
    public static void loadSettings() {
        Utils.loadFromJson(values, settingsFilename);
        updateCurrentSettings();
    }

    /// <summary>
    /// Updates all settings based on the current values of the backing
    /// data.
    /// </summary>
    public static void updateCurrentSettings() {
        Debug.Log("Updating all game settings...");
        // Apply default quality level settings first
        QualitySettings.SetQualityLevel(values.currentQuality, true);
        Application.targetFrameRate = values.targetFramerate;
        Screen.SetResolution(values.resolutionWidth, values.resolutionHeight, values.fullscreen);

        if (values.overrideQualitySettings) {
            QualitySettings.vSyncCount = values.vsyncCount;

            // Texture quality
            QualitySettings.anisotropicFiltering = values.anisotropicTextures;
            QualitySettings.antiAliasing = values.antialiasingSamples;

            // Lighting
            QualitySettings.pixelLightCount = values.pixelLightCount;
            QualitySettings.shadowDistance = values.shadowDistance;
            QualitySettings.shadowResolution = values.shadowResolution;
        } else {
            QualitySettings.vSyncCount = qualityPreset.vsyncCount;

            // Texture quality
            QualitySettings.anisotropicFiltering = qualityPreset.anisotropicTextures;
            QualitySettings.antiAliasing = qualityPreset.antialiasingSamples;

            // Lighting
            QualitySettings.pixelLightCount = qualityPreset.pixelLightCount;
            QualitySettings.shadowDistance = qualityPreset.shadowDistance;
            QualitySettings.shadowResolution = qualityPreset.shadowResolution;
        }
    }

    /// <summary>
    /// Reset configurable graphics settings to the quality defaults.
    /// </summary>
    public static void resetToCurrentQualityDefaults() {
        values.overrideQualitySettings = false;
        updateCurrentSettings();

        // Frame rate
        values.vsyncCount = QualitySettings.vSyncCount;
        values.targetFramerate = Application.targetFrameRate;

        // Texture quality
        values.anisotropicTextures = QualitySettings.anisotropicFiltering;
        values.antialiasingSamples = QualitySettings.antiAliasing;

        // Lighting
        values.pixelLightCount = QualitySettings.pixelLightCount;
        values.shadowDistance = QualitySettings.shadowDistance;
        values.shadowResolution = QualitySettings.shadowResolution;
    }

    /// <summary>
    /// Save settings to file and quit to desktop. Should be used whenever
    /// something needs to close the game.
    /// </summary>
    public static void quitToDesktop() {
        saveSettings();
        Application.Quit();
    }

    /// <summary>
    /// Cleans up the state manager and singletons. Should be used whenever
    /// something wants to return to the main menu for a blank slate, without
    /// closing the game entirely.
    /// </summary>
    public static void quitToMainMenu() {
        LoadingScreen loading = GameObject.FindGameObjectWithTag("Loading Screen")?.GetComponentInChildren<LoadingScreen>();

        // Destroy singletons; tracked by StateManager
        // Caution: don't add the load camera to the list!
        StateManager.destroyAllSingletons();
        saveSettings();
        StateManager.resetToDefaultState();

        // Load the right scene, using the loading screen if it is available
        if (loading != null) {
            loading.startLoadingScreen("Menu");
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
    }

    /// <summary>
    /// Get the mutex on the cursor state while locking the cursor
    ///
    /// <para>Remember to free it with unlockCursorState so the mutex is released!</para>
    /// </summary>
    /// <param name="caller">script requesting lock</param>
    /// <returns>whether the action was successful</returns>
    public static bool mutexLockCursorState(MonoBehaviour caller) {
        bool success = lockCursorState(caller);
        if (success) {
            cursorStateMutexOwner = caller;
        }
        return success;
    }

    /// <summary>
    /// Get the mutex on the cursor state while freeing the cursor
    ///
    /// <para>Remember to free it with lockCursorState so the mutex is released!</para>
    /// </summary>
    /// <param name="caller">script requesting unlock</param>
    /// <returns>whether the action was successful</returns>
    public static bool mutexUnlockCursorState(MonoBehaviour caller) {
        bool success = unlockCursorState(caller);
        if (success) {
            cursorStateMutexOwner = caller;
        }
        return success;
    }

    /// <summary>
    /// Attempts to lock the cursor state and disowns the mutex if the caller already owned it
    /// </summary>
    /// <param name="caller">script requesting lock</param>
    /// <returns>whether the action was successful</returns>
    public static bool lockCursorState(MonoBehaviour caller) {
        if (cursorStateMutexOwner == null || cursorStateMutexOwner == caller) {
            Cursor.lockState = CursorLockMode.Locked;
            cursorStateMutexOwner = null;
            return true;
        } else {
            Debug.LogWarning($"{caller} attempted to lock cursor state without owning the mutex!");
            return false;
        }
    }

    /// <summary>
    /// Attempts to unlock the cursor state and disowns the mutex if the caller already owned it
    /// </summary>
    /// <param name="caller">script requesting unlock</param>
    /// <returns>whether the action was successful</returns>
    public static bool unlockCursorState(MonoBehaviour caller) {
        if (cursorStateMutexOwner == null || cursorStateMutexOwner == caller) {
            Cursor.lockState = CursorLockMode.None;
            cursorStateMutexOwner = null;
            return true;
        } else {
            Debug.LogWarning($"{caller} attempted to unlock cursor state without owning the mutex!");
            return false;
        }
    }

    /// <summary>
    /// Violently unlocks the cursor state and evicts the existing mutex owner, if it was owned.
    ///
    /// <para>Do not use except when you NEED to impolitely alter controls, i.e., changing scenes.</para>
    /// </summary>
    public static void forceUnlockCursorState() {
        cursorStateMutexOwner = null;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Violently locks the cursor state and evicts the existing mutex owner, if it was owned.
    ///
    /// <para>Do not use except when you NEED to impolitely alter controls, i.e., changing scenes.</para>
    /// </summary>
    public static void forceLockCursorState() {
        cursorStateMutexOwner = null;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Default quality preset
    /// </summary>
    public static QualityPreset qualityPreset = DefaultQualitySettings.Default;

    /* stringified */
    public static string[] shadowResNames = {
        "Off", // warning: special case
        "Low",
        "Medium",
        "High",
        "Very High"
    };

    public static string[] anisotropicNames = {
        "Disabled",
        "Enabled",
        "Force Enabled"
    };

    public static string[] pixelQualityNames = {
        "None",
        "Low",
        "Medium",
        "High",
        "Ultra"
    };

    public static string[] vsyncCountNames = {
        "Disabled",
        "Every",
        "Every Other"
    };

    public static string[] antialiasingNames = {
        "Disabled",
        "2x MSAA",
        "4x MSAA",
        "8x MSAA"
    };

    /// <summary>
    /// Based on the index in the dropdown menu, get the actual Resolution
    /// object to update graphics settings.
    /// </summary>
    /// <param name="i">dropdown index selected</param>
    /// <returns>corresponding Resolution</returns>
    public static Resolution getResolutionByIndex(int i) {
        return Screen.resolutions[i];
    }

    /// <summary>
    /// Returns a human-readable list of strings for all supported resolutions.
    /// It's possible that this list has "duplicates" that are really different
    /// refresh rates, as the refresh rate isn't added to the display string.
    /// </summary>
    /// <returns></returns>
    public static List<string> getSupportedResolutions() {
        List<string> res = new List<string>();
        foreach (Resolution r in Screen.resolutions) {
            res.Add($"{r.width}x{r.height}");
        }
        return res;
    }
}

/// <summary>
/// Defaults for the configurable graphics settings for each quality preset.
/// Unity has no concept of "reset to the default of this quality preset,"
/// so here we go...
/// </summary>
public static class DefaultQualitySettings {
    public static QualityPreset VeryLow = new QualityPreset {
        vsyncCount = 0,
        anisotropicTextures = AnisotropicFiltering.Disable,
        antialiasingSamples = 0,
        pixelLightCount = 0,
        shadowDistance = 0,
        shadowResolution = ShadowResolution.Low
    };
    public static QualityPreset Low = new QualityPreset {
        vsyncCount = 0,
        anisotropicTextures = AnisotropicFiltering.Disable,
        antialiasingSamples = 0,
        pixelLightCount = 1,
        shadowDistance = 0,
        shadowResolution = ShadowResolution.Low
    };
    public static QualityPreset Medium = new QualityPreset {
        vsyncCount = 0,
        anisotropicTextures = AnisotropicFiltering.Enable,
        antialiasingSamples = 0,
        pixelLightCount = 1,
        shadowDistance = 20,
        shadowResolution = ShadowResolution.Low
    };
    public static QualityPreset Default = new QualityPreset {
        vsyncCount = 1,
        anisotropicTextures = AnisotropicFiltering.Enable,
        antialiasingSamples = 2,
        pixelLightCount = 1,
        shadowDistance = 40,
        shadowResolution = ShadowResolution.Low
    };
    public static QualityPreset High = new QualityPreset {
        vsyncCount = 1,
        anisotropicTextures = AnisotropicFiltering.Enable,
        antialiasingSamples = 2,
        pixelLightCount = 2,
        shadowDistance = 40,
        shadowResolution = ShadowResolution.Medium
    };
    public static QualityPreset VeryHigh = new QualityPreset {
        vsyncCount = 1,
        anisotropicTextures = AnisotropicFiltering.Enable,
        antialiasingSamples = 4,
        pixelLightCount = 3,
        shadowDistance = 70,
        shadowResolution = ShadowResolution.High
    };
    public static QualityPreset Ultra = new QualityPreset {
        vsyncCount = 1,
        anisotropicTextures = AnisotropicFiltering.Enable,
        antialiasingSamples = 4,
        pixelLightCount = 4,
        shadowDistance = 128,
        shadowResolution = ShadowResolution.VeryHigh
    };

    /// <summary>
    /// Get the appropriate quality preset (based on dropdown selection)
    /// </summary>
    /// <param name="i">dropdown index selection</param>
    /// <returns>corresponding QualityPreset</returns>
    public static QualityPreset getPresetByIndex(int i) {
        switch (i) {
            case 0:
                return VeryLow;
            case 1:
                return Low;
            case 2:
                return Medium;
            case 3:
                return Default;
            case 4:
                return High;
            case 5:
                return VeryHigh;
            default:
                return Ultra;
        }
    }
}

/// <summary>
/// Holds values of quality preset defaults. Unity does not make
/// quality preset values available at runtime, and any manually
/// modified settings are never reset.
/// </summary>
public struct QualityPreset {
    public int vsyncCount;
    public AnisotropicFiltering anisotropicTextures;
    public int antialiasingSamples;
    public int pixelLightCount;
    public float shadowDistance;
    public ShadowResolution shadowResolution;
}

/// <summary>
/// Serializable data class that can be saved to JSON.
/// </summary>
[System.Serializable]
public class SettingsValues {
    /// <summary>
    /// Whether the story tutorial scene has been completed.
    /// </summary>
    public bool sawStoryTutorial = false;

    /// <summary>
    /// Whether the loan tutorial screen has been viewed.
    /// </summary>
    public bool sawLoanTutorial = false;

    /// <summary>
    /// Whether the weapon tutorial screen has been viewed.
    /// </summary>
    public bool sawShopTutorial = false;

    /// <summary>
    /// Current game volume. Range: [0, 1]
    /// </summary>
    public float volume = 0.5f;

    /// <summary>
    /// Current mouse sensitivity.
    /// </summary>
    public float mouseSensitivity = 100f;

    // ------------- keybinds ---------------
    public string pauseKey = "escape";
    public string interactKey = "f";
    public string weaponOneKey = "1";
    public string weaponTwoKey = "2";
    public string hidePauseMenuKey = "backspace";

    /// <summary>
    /// Whether to show the current FPS.
    /// </summary>
    public bool showFPS = false;

    // --- configurable graphics ---
    /// <summary>
    /// Whether configured graphics settings should override the selected
    /// quality preset's settings.
    /// </summary>
    public bool overrideQualitySettings = false;

    /// <summary>
    /// Target framerate. Only works when vsyncCount = 0, otherwise,
    /// the frame rate is affected by the monitor refresh rate.
    /// </summary>
    public int targetFramerate = 60;

    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public bool fullscreen = false;
    public int vsyncCount = 1;  // 0, 1, 2, 3, 4
    public AnisotropicFiltering anisotropicTextures = AnisotropicFiltering.Disable;  // Disable, Enable, ForceEnable
    public int antialiasingSamples = 2;  // 0, 2, 4, 8 only supported
    public int pixelLightCount = 1;  // 0, 1, 2, 3, 4; 0 = dark, no pixel lights!
    public float shadowDistance = 40;  // maximum distance to draw shadows at
    public ShadowResolution shadowResolution = ShadowResolution.Low;  // Low, Medium, High, VeryHigh

    /* Very Low, Low, Medium, [Default], High, Very High, Ultra */
    /* Save the strings so they're available in the settings.json for users
       who break stuff and want to reset to defaults.
    */
    public string[] qualityLevelNames = QualitySettings.names;
    public int currentQuality = 3;
    public string currentQualityName = "Default";
}
