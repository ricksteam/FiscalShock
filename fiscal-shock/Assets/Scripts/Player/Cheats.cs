using UnityEngine;
using FiscalShock.Demo;
using FiscalShock.GUI;

/// <summary>
/// Enables some debug commands/"cheats" used to help developers
/// test or demonstrate the game.
/// </summary>
public class Cheats : MonoBehaviour {
    [Tooltip("Key to press to teleport to the escape portal. Must be lowercase, as Unity's old input system expects it.")]
    public string teleportToEscapeKey = "f2";

    [Tooltip("Key to press to teleport to the delve portal. Must be lowercase, as Unity's old input system expects it.")]
    public string teleportToDelveKey = "f3";

    [Tooltip("Key to press to add cash. Must be lowercase, as Unity's old input system expects it.")]
    public string robinHood = "f1";

    [Tooltip("Key to press to teleport to toggle the graph rendering. Must be lowercase, as Unity's old input system expects it.")]
    public string toggleGraphMesh = "f4";

    [Tooltip("Key to press to enable wall destruction. Must be lowercase, as Unity's old input system expects it.")]
    public string enableWallDestruction = "f8";

    /// <summary>
    /// Reference to the player movement script. Needed to use the
    /// teleport function.
    /// </summary>
    public PlayerMovement playerMovement { get; set; }

    /// <summary>
    /// Whether the player can currently destroy walls.
    /// </summary>
    public bool destroyWalls;

    /// <summary>
    /// Handle input on each frame.
    /// </summary>
    private void Update() {
        if (Input.GetKeyDown(teleportToEscapeKey)) {
            GameObject escape = GameObject.Find("Escape Point");
            Vector3 warpPoint = escape.transform.position;
            playerMovement.teleport(new Vector3(warpPoint.x - Random.Range(-2, 2), warpPoint.y + 4, warpPoint.z + Random.Range(-2, 2)));
        }
        if (Input.GetKeyDown(teleportToDelveKey)) {
            GameObject delve = GameObject.Find("Delve Point");
            Vector3 warpPoint = delve.transform.position;
            playerMovement.teleport(new Vector3(warpPoint.x - Random.Range(-2, 2), warpPoint.y + 4, warpPoint.z + Random.Range(-2, 2)));
        }
        if (Input.GetKeyDown(robinHood)) {
            StateManager.cashOnHand += 500;
            Debug.Log("Added 500 monies");
        }
        if (Input.GetKeyDown(toggleGraphMesh)) {
            ProceduralMeshRenderer pmr = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<ProceduralMeshRenderer>();
            pmr.enabled = !pmr.enabled;
            if (!pmr.enabled) {
                GameObject go = GameObject.Find("Vertices Display");
                Destroy(go);
                pmr.alreadyDrew = false;
            }
            Debug.Log($"Toggled mesh view to {pmr.enabled}");
        }
        if (Input.GetKeyDown(enableWallDestruction)) {
            destroyWalls = !destroyWalls;
            Debug.Log($"Toggled wall destruction: {destroyWalls}");
        }
    }
}
