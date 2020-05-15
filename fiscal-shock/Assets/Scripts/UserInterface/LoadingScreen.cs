using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles asynchronous loading of scenes, so we can have a loading screen
/// that displays loading progress (to the best of Unity's ability).
/// Adapted from https://gist.github.com/nickpettit/a78cc0a9483c85212a23
/// </summary>
public class LoadingScreen : MonoBehaviour {
    /// <summary>
    /// Reference to the next scene to load. Must be the filename of the
    /// scene to load.
    /// </summary>
    private string nextScene = "Hub";

    [Tooltip("Reference to the main loading text body.")]
    public TextMeshProUGUI loadingText;

    [Tooltip("Reference to the smaller instructional text at the bottom of the loading screen")]
    public TextMeshProUGUI clickText;

    [Tooltip("Reference to the slider GUI element used to create a progress bar effect")]
    public Slider progressBar;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static LoadingScreen loadScreenInstance { get; private set; }

    /// <summary>
    /// Reference to the current async loading operation
    /// </summary>
    private AsyncOperation async;

    [Tooltip("Reference to the loading screen GUI panel")]
    public Canvas loadCanvas;

    [Tooltip("Color of the progress bar when the next level is still loading")]
    public Color loadingColor;

    [Tooltip("Color of the progress bar when the next level has finished loading as much as it can asynchronously")]
    public Color doneColor;

    [Tooltip("Image used for the progress bar fill. Gets colored by the aforementioned colors.")]
    public Image progressFill;

    [Tooltip("Reference to text object displaying the progress of loading as a percent")]
    public TextMeshProUGUI percentText;

    /// <summary>
    /// Last scene loaded
    /// </summary>
    public string previousScene { get; private set; }

    [Tooltip("Reference to the tombstone image. Used when the player has been defeated.")]
    public GameObject tombstone;

    /// <summary>
    /// Story text to display when the player is loading the temple level.
    /// </summary>
    private readonly string templeStory = "Hostile robots are excavating the Ruins of Tehamahouti, stealing every shiny object they can get their hands on. Clear out the robots before it becomes a total archaeological loss! Oh, and try not to die.";

    /// <summary>
    /// Story text to display when the player is loading the mines level.
    /// </summary>
    private readonly string mineStory = "We have traced a cache of black market gold and gemstones to a series of mines. Naturally, BOTCORP is the culprit. Due to the CEO's affiliation and close ties with illegal markets, we believe that he is storing stolen artifacts for resale here, as well. Your job is the same as always: crush the bots. Our specialists will come in and take care of the rest.";

    /// <summary>
    /// Default text to display on loading. Used when the story text isn't.
    /// </summary>
    private readonly string defaultText = "Loading...";

    /// <summary>
    /// List of phrases to display on the tombstone when the player is defeated
    /// </summary>
    private readonly string[] eulogies = {
        "Suffocated under a pile of loans",
        "peperony and chease",
        "Couldn't pay loans with charm and good looks",
        "Can't pay off a payday loan if you don't get paid.",
        "Can't pay off a payday loan if you don't survive 'til payday.",
        "Bury me with my money!",
        "YOU DIED",
        "That financial plan didn't make much cents.",
        "That wasn't a wise investment.",
        "Bought the farm with all those loans",
        "Paid more than an arm and a leg",
        "Deadbeat",
        "Derailed the gravy train",
        "Really in the hole now",
        "Couldn't make a living",
        "Paid the piper",
        "Dead broke"
    };

    /// <summary>
    /// Prevent restarting an async load
    /// </summary>
    public bool loading = false;


    /// <summary>
    /// Singleton management. Note that this singleton isn't added to the state
    /// manager's list. We still want to use the loading screen when possible.
    /// </summary>
    private void Awake() {
        if (loadScreenInstance != null && loadScreenInstance != this) {
            Destroy(this.gameObject);
            return;
        }
        else {
            loadScreenInstance = this;
        }
        DontDestroyOnLoad(this.gameObject);
        // Do not add to StateManager.singletons unless you never want to use the load screen to get to the menu
    }

    /// <summary>
    /// Initialize variables and hide the loading screen by default.
    /// </summary>
    private void Start() {
        loadCanvas.enabled = false;
        loadingText.text = defaultText;
        clickText.enabled = false;
    }

    /// <summary>
    /// Updates the progress bar each frame when a level is loading.
    /// Also handles input to enable activation of the next scene.
    /// </summary>
    private void Update() {
        // If the new scene has started loading...
        if (async != null) {
            // ...then pulse the transparency of the loading text to let the player know that the computer is still working.
            progressBar.value = Mathf.Clamp01(async.progress / 0.9f);
            if (progressBar.value > 0.9f && !clickText.enabled) {
                progressFill.color = doneColor;
                clickText.enabled = true;
            }
            percentText.text = $"{(int)(progressBar.value * 100)}%";
            if (((Input.GetMouseButtonDown(0) && async.progress > 0f) || (async.progress > 0.8f && nextScene != "Dungeon" && nextScene != "LoseGame")) && !async.allowSceneActivation) {
                Debug.Log($"Allowing scene activation for {nextScene}");
                async.allowSceneActivation = true;
                clickText.text = "Please wait...";
                clickText.enabled = true;
            }
            if (async.allowSceneActivation && nextScene == "Dungeon") {
                StartCoroutine(restartTime());
            }
        }
    }

    /// <summary>
    /// Restarts time asynchronously by waiting real-time seconds. Some
    /// race conditions and bad things seemed to happen when time was
    /// instantly enabled.
    /// </summary>
    private IEnumerator restartTime() {
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 1;
        yield return null;
    }

    /// <summary>
    /// Disables any scripts passed in and then starts the coroutine to begin
    /// asynchronous loading of the next scene while keeping a loading screen
    /// on. Always disable the player scripts and enemy scripts.
    /// </summary>
    /// <param name="sceneToLoad"></param>
    public void startLoadingScreen(string sceneToLoad) {
        if (loading) {
            return;
        }
        loading = true;
        StateManager.pauseAvailable = false;
        System.GC.Collect();
        previousScene = SceneManager.GetActiveScene().name;
        nextScene = sceneToLoad;
        StartCoroutine(loadScene());
    }

    /// <summary>
    /// Main function to start loading a scene. Updates the load screen text
    /// and starts the actual scene load.
    /// </summary>
    private IEnumerator<WaitForSeconds> loadScene() {
        Debug.Log("Scene load starting");
        tombstone.SetActive(false);
        loadCanvas.enabled = true;
        progressBar.value = 0;
        progressFill.color = loadingColor;
        Time.timeScale = 0;
        async = SceneManager.LoadSceneAsync(nextScene);
        if (nextScene == "Dungeon") {
            switch (StateManager.selectedDungeon) {
                case DungeonTypeEnum.Temple:
                    async.allowSceneActivation = false;
                    loadingText.text = templeStory;
                    break;
                case DungeonTypeEnum.Mine:
                    async.allowSceneActivation = false;
                    loadingText.text = mineStory;
                    break;
                default:
                    loadingText.text = defaultText;
                    break;
            }
        } else if (!StateManager.playerDead) {
            loadingText.text = defaultText;
            async.allowSceneActivation = true;
        } else {  // Dead
            loadingText.text = "";
            tombstone.GetComponentInChildren<TextMeshProUGUI>().text = $"{eulogies[Random.Range(0, eulogies.Length)]}";
            tombstone.SetActive(true);
            async.allowSceneActivation = false;
        }

        while (!async.isDone) {
            yield return null;
        }
        Debug.Log("Scene load is done");

        loading = false;
        async = null;
        loadCanvas.enabled = false;
        StateManager.pauseAvailable = true;
        loadingText.text = defaultText;
        clickText.enabled = false;
        clickText.text = "Press the Left Mouse Button to continue.";
    }
}
