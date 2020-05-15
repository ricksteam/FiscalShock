using UnityEngine;

/// <summary>
/// Singleton that prevents things from being destroyed on scene changes.
/// Must be manually destroyed if it needs to go away.
/// Using a singleton music player for the dungeon allows music to continue
/// playing, even after a new scene is loaded.
/// </summary>
public class SeamlessMusicPlayer : MonoBehaviour {
    public static SeamlessMusicPlayer musicInstance { get; private set; }

    private void Awake() {
        if (musicInstance != null && musicInstance != this) {
            Destroy(this.gameObject);
            return;
        } else {
            musicInstance = this;
        }
        DontDestroyOnLoad(this.gameObject);
        StateManager.singletons.Add(this.gameObject);
    }
}
