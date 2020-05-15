using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages visual feedback of money gain/loss by displaying numbers on the
/// screen when certain events happen.
/// </summary>
public class FeedbackController : MonoBehaviour
{
    /// <summary>
    /// Object pool for the money loss (spent a bullet) feedback
    /// </summary>
    private Queue<TextMeshProUGUI> shotLosses { get; } = new Queue<TextMeshProUGUI>();

    /// <summary>
    /// Maximum number of shot loss feedback objects to display at once; also
    /// the size of the object pool. Should be high enough for automatic weapons
    /// that can fire once per frame, with the game capped at 60 FPS.
    /// </summary>
    private int numLossesToDisplay = 60;

    /// <summary>
    /// Object pool for money gain (defeated an enemy) feedback
    /// </summary>
    private Queue<TextMeshProUGUI> earns { get; } = new Queue<TextMeshProUGUI>();

    /// <summary>
    /// Maximum number of money gain feedback objects to display at once; also
    /// the size of the object pool. Should be high enough for most situations.
    /// Consider how many enemies the player might defeat at once, or within
    /// 2 seconds.
    /// </summary>
    private int numEarnsToDisplay = 16;

    [Tooltip("Reference to the money loss text object to be cloned")]
    public TextMeshProUGUI shotLoss;

    [Tooltip("Reference to the money gain text object to be cloned")]
    public TextMeshProUGUI earn;

    [Tooltip("Reference to the HUD hit vignette")]
    public Image hitVignette;

    /// <summary>
    /// Create object pools
    /// </summary>
    private void Start() {
        for (int i = 0; i < numLossesToDisplay; ++i) {
            TextMeshProUGUI sh = Instantiate(shotLoss);
            sh.transform.SetParent(transform);
            sh.enabled = false;
            shotLosses.Enqueue(sh);
        }
        for (int i = 0; i < numEarnsToDisplay; ++i) {
            TextMeshProUGUI ea = Instantiate(earn);
            ea.transform.SetParent(transform);
            ea.enabled = false;
            earns.Enqueue(ea);
        }
    }

    /// <summary>
    /// Creates the feedback on the screen for money lost when shooting.
    /// </summary>
    /// <param name="cost"></param>
    public void shoot(float cost) {
        TextMeshProUGUI clone = shotLosses.Dequeue();
        clone.text = "-" + (cost.ToString("F0"));
        clone.color = new Color(clone.color.r, clone.color.g, clone.color.b, 1f);
        clone.transform.localPosition = new Vector3(Screen.width*-0.15f, 0, 0);
        clone.transform.Translate(Random.Range(-50f, 0), Random.Range(-50f, 50f), Random.Range(-50f, 50f), Space.Self);
        clone.enabled = true;
        shotLosses.Enqueue(clone);

        StartCoroutine(timeout(clone, 2f));
    }

    /// <summary>
    /// Creates the feedback on the screen for money gained from killing
    /// enemies.
    /// </summary>
    /// <param name="amount"></param>
    public void profit(float amount) {
        TextMeshProUGUI clone = earns.Dequeue();
        clone.text = "+" + (amount.ToString("F0"));
        clone.color = new Color(clone.color.r, clone.color.g, clone.color.b, 1f);
        clone.transform.localPosition = new Vector3(Screen.width*0.4f, 0, 0);
        clone.transform.Translate(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f), Space.Self);
        clone.enabled = true;
        earns.Enqueue(clone);

        StartCoroutine(timeout(clone, 2f));
    }

    /// <summary>
    /// Asynchronous function to fade the text objects over time.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator timeout(TextMeshProUGUI text, float duration) {
        for (float i = duration; i >= 0; i -= Time.deltaTime) {
            text.color = new Color(text.color.r, text.color.g, text.color.b, i/duration);
            yield return null;
        }
        text.enabled = false;
        yield return null;
    }
}
