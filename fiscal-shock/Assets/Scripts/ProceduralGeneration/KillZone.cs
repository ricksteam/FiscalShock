using UnityEngine;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Trigger zone that sends the player to the lose screen if they happen to
    /// jump off the edge of the world. With changes to the dungeon generation
    /// algorithm, it's no longer possible for the player to enter this kill
    /// zone during normal gameplay.
    /// </summary>
    public class KillZone : MonoBehaviour {
        private GameObject loadingScreen;
        private LoadingScreen loadScript;

        private void Start() {
            loadingScreen = GameObject.FindGameObjectWithTag("Loading Screen");
            loadScript = loadingScreen.GetComponent<LoadingScreen>();
        }

        private void OnTriggerEnter(Collider collider) {
            if (collider.gameObject.tag == "Player") {
                StateManager.playerDead = true;
                GameObject musicPlayer = GameObject.Find("DungeonMusic");
                Destroy(musicPlayer);
                StateManager.startNewDay();
                loadScript.startLoadingScreen("LoseGame");
            }
        }
    }
}
