﻿using UnityEngine;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Behavior of the portal that sends the player deeper into the dungeon.
    /// </summary>
    public class Delve : MonoBehaviour {
        private GameObject loadingScreen;
        private LoadingScreen loadScript;
        private Animation anim;

        private void Start() {
            loadingScreen = GameObject.FindGameObjectWithTag("Loading Screen");
            loadScript = loadingScreen.GetComponent<LoadingScreen>();
            // Play the arrow spinning animation
            anim = gameObject.GetComponentInChildren<Animation>();
            anim["Delve"].speed = 0.8f;
        }

        private void Update() {
            if (anim.isPlaying) {
                return;
            }
            anim.Play("Delve");
        }

        private void OnTriggerEnter(Collider collider) {
            if (collider.gameObject.tag == "Player") {
                StateManager.totalFloorsVisited++;
                StateManager.currentFloor++;
                loadScript.startLoadingScreen("Dungeon");
            }
        }
    }
}
