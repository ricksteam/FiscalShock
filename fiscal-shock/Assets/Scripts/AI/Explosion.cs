using UnityEngine;
using System.Collections;

/// <summary>
/// Behavior of visual feedback particle effects when enemies are struck.
/// </summary>
public class Explosion : MonoBehaviour {
    [Tooltip("How long the explosion should remain visible, in seconds.")]
    public float lifetime = 0.9f;

    /// <summary>
    /// Return the object to the pool when done.
    /// </summary>
    public IEnumerator timeout() {
        yield return new WaitForSeconds(lifetime);
        gameObject.SetActive(false);
        yield return null;
    }
}
