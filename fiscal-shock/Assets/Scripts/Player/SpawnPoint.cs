using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles placement of the player on scene changes.
/// </summary>
public class SpawnPoint : MonoBehaviour {
    [Tooltip("Whether this spawnpoint should update the player position as soon as it loads.")]
    public bool autoSpawn = true;

    [Tooltip("Reference to the player prefab. Needed to instantiate a player if it doesn't exist already.")]
    public GameObject playerPrefab;

    /// <summary>
    /// Singleton instance management
    /// </summary>
    private static SpawnPoint spawnPointInstance;

    /// <summary>
    /// Default position to spawn the player at in the hub
    /// </summary>
    private Vector3 defaultHubPos = new Vector3(3.117362f, 1.2f, -7.210602f);

    /// <summary>
    /// Default rotation the player should face in the hub
    /// </summary>
    private Quaternion defaultHubRotation = Quaternion.Euler(0, 90, 0);

    /// <summary>
    /// Singleton handling of the spawn point
    /// </summary>
    private void Awake() {
        if (spawnPointInstance != null && spawnPointInstance != this) {
            Destroy(this.gameObject);
            return;
        } else {
            spawnPointInstance = this;
        }
        DontDestroyOnLoad(this.gameObject);
        StateManager.singletons.Add(gameObject);
    }

    /// <summary>
    /// Add a listener to the sceneLoaded event when this is enabled
    /// </summary>
    private void OnEnable() {
        SceneManager.sceneLoaded += onSceneLoad;
    }

    /// <summary>
    /// Remove the listener to the sceneLoaded event when this is disabled
    /// </summary>
    private void OnDisable() {
        SceneManager.sceneLoaded -= onSceneLoad;
    }

    /// <summary>
    /// Event called when a scene is loaded. The arguments are not used, but
    /// required for this to be assigned as a delegate.
    /// </summary>
    /// <param name="s">default argument for sceneLoaded delegate</param>
    /// <param name="ss">default argument for sceneLoaded delegate</param>
    private void onSceneLoad(Scene s, LoadSceneMode ss) {
        if (autoSpawn) {
            spawnPlayer();
            Settings.forceLockCursorState();
        }
    }

    /// <summary>
    /// Simulate a sceneLoaded event when this script is started, since it will
    /// miss the initial sceneLoaded event.
    /// </summary>
    private void Start() {
        onSceneLoad(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    /// <summary>
    /// Reset things to hub defaults.
    /// </summary>
    public void resetToHubDefaults() {
        transform.position = defaultHubPos;
        transform.rotation = defaultHubRotation;
        autoSpawn = true;
        GameObject.FindGameObjectWithTag("Player Flashlight").GetComponent<Light>().intensity = 0;
        PlayerShoot shootScript = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<PlayerShoot>();
        shootScript.enabled = false;
    }

    /// <summary>
    /// Instantiate a new player object. Should only be called if there wasn't
    /// one already!
    /// </summary>
    /// <returns>player GameObject</returns>
    public GameObject spawnNewPlayer() {
        if (transform.position == Vector3.zero) {
            Debug.LogError($"No spawn point was set! Defaulting to {transform.position}");
        }
        return Instantiate(playerPrefab, transform.position, transform.rotation);
    }

    /// <summary>
    /// Updates the player's position ("spawns the player").
    /// </summary>
    /// <returns>player GameObject</returns>
    public GameObject spawnPlayer() {
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer == null) {
            return spawnNewPlayer();
        }
        if (transform.position == Vector3.zero) {
            Debug.LogError($"No spawn point was set! Defaulting to {transform.position}");
        }
        existingPlayer.GetComponentInChildren<PlayerMovement>().teleport(transform.position);
        existingPlayer.transform.rotation = transform.rotation;
        Debug.Log($"Spawned player at {transform.position}");

        return existingPlayer;
    }
}
