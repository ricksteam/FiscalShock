using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FiscalShock.GUI {
    /// <summary>
    /// Handles dungeon selection and display of the dungeon selection screen.
    /// </summary>
    public class DungeonSelection : MonoBehaviour {
        /// <summary>
        /// Whether the player can interact with the dungeon selection screen.
        /// </summary>
        private bool isPlayerInTriggerZone = false;

        /// <summary>
        /// Reference to the loading screen object.
        /// </summary>
        private GameObject loadingScreen;

        /// <summary>
        /// Reference to the loading screen script.
        /// </summary>
        private LoadingScreen loadScript;

        [Tooltip("Reference to the GUI canvas for dungeon selection.")]
        public Canvas selectionScreen;

        [Tooltip("Sound effect to play when entry is denied.")]
        public AudioClip bummer;

        [Tooltip("Reference to the canvas that owns the instructional text.")]
        public Canvas textCanvas;

        [Tooltip("Instructional text to display when the player is near the dungeon entrance.")]
        public TextMeshProUGUI texto;

        /// <summary>
        /// Default instructional text. Saved, so it can be reset when the
        /// player walks out of the trigger zone.
        /// </summary>
        private string originalText;

        /// <summary>
        /// Reference to an audio source to play sounds off of.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// Initialize references and hide the screen/text.
        /// </summary>
        private void Start() {
            Time.timeScale = 1;  // sorry but it won't restart in the hub rightly
            loadingScreen = GameObject.FindGameObjectWithTag("Loading Screen");
            loadScript = loadingScreen.GetComponent<LoadingScreen>();
            selectionScreen.enabled = false;
            audioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Handle collision events
        /// </summary>
        /// <param name="col">collider that entered the trigger zone</param>
        private void OnTriggerEnter(Collider col) {
            if (col.gameObject.tag == "Player") {
                isPlayerInTriggerZone = true;
                originalText = texto.text;
            }
        }

        /// <summary>
        /// Handle collision events
        /// </summary>
        /// <param name="col">collider that exited the trigger zone</param>
        private void OnTriggerExit(Collider col) {
            if (col.gameObject.tag == "Player") {
                isPlayerInTriggerZone = false;
                texto.text = originalText;
            }
        }

        /// <summary>
        /// Updates each frame to handle input events.
        /// </summary>
        private void FixedUpdate() {
            if (isPlayerInTriggerZone) {
                if (Input.GetKeyDown(Settings.interactKey)) {
                    if (GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<PlayerShoot>().guns.Count < 1) {
                        audioSource.PlayOneShot(bummer, Settings.volume);
                        texto.text = "It's dangerous to go out alone (and unarmed).";
                        return;
                    }
                    StateManager.pauseAvailable = false;
                    Settings.forceUnlockCursorState();
                    textCanvas.enabled = false;
                    selectionScreen.enabled = true;
                    StateManager.pauseAvailable = false;
                }
                if (Input.GetKeyDown(Settings.pauseKey)) {
                    closeSelectionScreen();
                }
            }
        }

        /// <summary>
        /// Callback to hide the dungeon selection screen. Should be attached to
        /// the GUI close button OnClick.
        /// </summary>
        public void closeSelectionScreen() {
            selectionScreen.enabled = false;
            Settings.mutexLockCursorState(this);
            StateManager.pauseAvailable = true;
            if (isPlayerInTriggerZone) {
                textCanvas.enabled = true;
            }
        }

        /// <summary>
        /// Callback to select a dungeon. Should be attached to the GUI dungeon
        /// buttons OnClick.
        /// </summary>
        /// <param name="value"></param>
        public void selectDungeonStart(int value){
            selectDungeon(value);
        }

        /// <summary>
        /// Main function to pick a dungeon, update the state manager, and hand
        /// things off to the loading screen.
        /// </summary>
        /// <param name="value"></param>
        public void selectDungeon(int value) {
            selectionScreen.enabled = false;
            StateManager.selectedDungeon = (DungeonTypeEnum)value;
            StateManager.cashOnEntrance = StateManager.cashOnHand;
            StateManager.timesEntered++;
            StateManager.totalFloorsVisited++;
            StateManager.currentFloor = 1;
            StateManager.startedFromDungeon = false;
            Settings.forceLockCursorState();
            StartCoroutine(StateManager.makePauseAvailableAgain());
            loadScript.startLoadingScreen("Dungeon");
        }
    }
}
