using UnityEngine;

/// <summary>
/// Enables controlling the volume of an audio source like a music
/// player that doesn't fire off sound effects via script.
/// Used by InGameSettings when volume is changed.
/// </summary>
public class VolumeController : MonoBehaviour {
    public AudioSource audioS { get; private set; }

    private void Start() {
        audioS = GetComponent<AudioSource>();
        audioS.volume = Settings.volume;
    }
}
