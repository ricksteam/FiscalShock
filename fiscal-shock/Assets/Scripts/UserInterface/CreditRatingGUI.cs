using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// GUI visualization for the player's credit score/credit rating.
/// </summary>
public class CreditRatingGUI : MonoBehaviour {
    [Tooltip("Reference to the bar section for abysmal (bottom)")]
    public Image abysmal;

    [Tooltip("Reference to the bar section for poor")]
    public Image poor;

    [Tooltip("Reference to the bar section for fair")]
    public Image fair;

    [Tooltip("Reference to the bar section for good")]
    public Image good;

    [Tooltip("Reference to the bar section for excellent (top)")]
    public Image excellent;

    [Tooltip("Reference to text object that displays the player's credit rating")]
    public TextMeshProUGUI texto;

    [Tooltip("Reference to the text object that displays the difference between the last credit score and the current credit score")]
    public TextMeshProUGUI delta;

    /// <summary>
    /// Delayed update of the rating bar. There were race conditions when the
    /// bar was updated immediately. This does lead to the bar showing invalid
    /// data right after the player loads into the hub for the first time, if
    /// they pause immediately. It will be updated after 1 second passes while
    /// time is flowing.
    /// </summary>
    private void Awake() {
        Invoke("updateRatingBar", 1f);
    }

    /// <summary>
    /// Update the state manager reference when this script is enabled.
    /// </summary>
    private void OnEnable() {
        StateManager.creditBarScript = this;
    }

    /// <summary>
    /// Refresh the credit rating bar on the pause menu.
    /// </summary>
    public void updateRatingBar() {
        // The images should all have vertical fill enabled, so we can give a visualization of the current credit score, without the player needing to worry about an exact value.
        excellent.fillAmount = Mathf.Clamp01((StateManager.creditScore - StateManager.ExcellentCredit.min) / StateManager.ExcellentCredit.range);
        good.fillAmount = Mathf.Clamp01((StateManager.creditScore - StateManager.GoodCredit.min) / StateManager.GoodCredit.range);
        fair.fillAmount = Mathf.Clamp01((StateManager.creditScore - StateManager.FairCredit.min) / StateManager.FairCredit.range);
        poor.fillAmount = Mathf.Clamp01((StateManager.creditScore - StateManager.PoorCredit.min) / StateManager.PoorCredit.range);
        abysmal.fillAmount = Mathf.Clamp01((StateManager.creditScore - StateManager.AbysmalCredit.min) / StateManager.AbysmalCredit.range);

        // C# can't compare structs...
        if (StateManager.currentRating.rating == StateManager.ExcellentCredit.rating) {
            texto.color = excellent.color;
        } else if (StateManager.currentRating.rating == StateManager.GoodCredit.rating) {
            texto.color = good.color;
        } else if (StateManager.currentRating.rating == StateManager.FairCredit.rating) {
            texto.color = fair.color;
        } else if (StateManager.currentRating.rating == StateManager.PoorCredit.rating) {
            texto.color = poor.color;
        } else if (StateManager.currentRating.rating == StateManager.AbysmalCredit.rating) {
            texto.color = abysmal.color;
        }
        texto.text = StateManager.currentRating.rating;

        // Update the change from the last score to the new score
        int deltint = Mathf.RoundToInt(StateManager.creditScore - StateManager.lastCreditScore);
        delta.text = $"{(deltint > 0? "+" : "")}{deltint}";
        if (StateManager.lastCreditScore < StateManager.AbysmalCredit.min) {
            delta.text = "";
        }
        if (deltint > 0) {
            delta.color = excellent.color;
        } else if (deltint < 0) {
            delta.color = abysmal.color;
        } else {
            delta.color = Color.white;
        }

        StartCoroutine(showCreditDelta(deltint));
    }

    /// <summary>
    /// Display and fade text over time with the credit score change after the
    /// player leaves the dungeon.
    /// </summary>
    /// <param name="deltint">change in credit score since last check</param>
    private IEnumerator showCreditDelta(int deltint) {
        if (StateManager.income.Count > 0) {
            TextMeshProUGUI pt = GameObject.FindGameObjectWithTag("Player Text").GetComponent<TextMeshProUGUI>();
            if (deltint > 0) {
                pt.color = Color.green;
            } else if (deltint < 0) {
                pt.color = Color.red;
            } else {
                pt.color = Color.white;
            }
            pt.text = $"Credit score change:\n{(deltint > 0? "+" : "")}{deltint}";
            for (float i = 2f; i >= 0; i -= Time.deltaTime) {
                pt.color = new Color(pt.color.r, pt.color.g, pt.color.b, i/2f);
                yield return null;
            }
            pt.color = new Color(pt.color.r, pt.color.g, pt.color.b, 0);
        }
        yield return null;
    }
}
