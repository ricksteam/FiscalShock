using UnityEngine;
using TMPro;

/// <summary>
/// Displays some text on a game view if the player is close enough to the
/// sign object. Should eventually be replaced with updating the text object
/// on the player itself at appropriate times, and not reliant on separate
/// sign objects scattered around the world.
/// </summary>
public class Sign : MonoBehaviour {
    [Tooltip("Canvas to enable when the player is close enough")]
    public Canvas canvas;

    [Tooltip("Reference to text object")]
    public TextMeshProUGUI signText;

    /// <summary>
    /// Assign references, if they were not explicitly assigned in the
    /// inspector. Also hides the text and replaces some strings with
    /// the actual keys.
    /// </summary>
    private void Start() {
        if (canvas == null) {
            canvas = GetComponentInChildren<Canvas>();
        }
        canvas.enabled = false;
        if (signText == null) {
            signText = GetComponentInChildren<TextMeshProUGUI>();
        }
        signText.text = signText.text.Replace("INTERACTKEY", Settings.interactKey.ToUpper()).Replace("PAUSEKEY", Settings.pauseKey.ToUpper());
    }

    /// <summary>
    /// Display the text if the player is close enough.
    /// </summary>
    /// <param name="col">collider that entered the trigger zone</param>
    private void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "Player") {
            canvas.enabled = true;
        }
    }

    /// <summary>
    /// Hide the text if the player walks away.
    /// </summary>
    /// <param name="col">collider that exited the trigger zone</param>
    private void OnTriggerExit(Collider col) {
        if (col.gameObject.tag == "Player") {
            canvas.enabled = false;
        }
    }
}
