using UnityEngine;
using TMPro;

/// <summary>
/// Class to handle some input field events for better visual
/// feedback. The public void functions should be attached to
/// GUI events on the TM_InputField objects.
/// </summary>
public class InputFieldEvents : MonoBehaviour {
    /// <summary>
    /// Original text that was in this field.
    /// </summary>
    private string originalText;

    [Tooltip("Reference to the placeholder text of this text field.")]
    public TextMeshProUGUI placeholder;

    /// <summary>
    /// Store the original text.
    /// </summary>
    private void Start() {
        originalText = placeholder.text;
    }

    /// <summary>
    /// Hide the placeholder text. Should be called when the input field is
    /// focused/selected.
    /// </summary>
    public void hidePlaceholder() {
        placeholder.text = "";  // clear out the text
    }

    /// <summary>
    /// Replace the current text with the original placeholder text. Should
    /// be called when the input field is cleared out.
    /// </summary>
    public void showPlaceholder() {
        placeholder.text = originalText;
    }
}
