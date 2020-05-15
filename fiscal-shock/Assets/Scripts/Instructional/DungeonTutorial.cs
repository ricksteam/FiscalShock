using UnityEngine;

/// <summary>
/// Displays a tutorial GUI when the player first enters the dungeon.
/// The tutorial explains to the player what signs to look for to
/// determine what objects are portals. Currently, portal models have
/// a red animated arrow if they lead to a new dungeon level, or a
/// green animated arrow if they lead back to the hub.
/// Initial user feedback showed that players were unable to infer
/// what each portal did without this instruction.
/// </summary>
public class DungeonTutorial : MonoBehaviour {

    /// <summary>
    /// Show the tutorial if the player hasn't seen it yet
    /// </summary>
    private void Start() {
        if (!StateManager.sawEntryTutorial) {
            // Pause game
            Settings.mutexUnlockCursorState(this);
            Time.timeScale = 0;
        } else {
            GetComponent<Canvas>().enabled = false;
        }
    }

    /// <summary>
    /// Handle input events each frame
    /// </summary>
    private void Update() {
        if (!StateManager.sawEntryTutorial) {
            Time.timeScale = 0;  // failsafe due to async in load screen
        }
        if (Input.GetKeyDown(Settings.pauseKey)) {
            dismissWindow();
        }
    }

    /// <summary>
    /// Dismisses this GUI and updates the state so that it's not shown
    /// next time a dungeon level is loaded.
    /// </summary>
    public void dismissWindow() {
        GetComponent<Canvas>().enabled = false;
        Settings.lockCursorState(this);
        StateManager.sawEntryTutorial = true;
        Time.timeScale = 1;
    }
}
