using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the heads-up display that imparts all of the immediately
/// useful (and most important to know) information to the player.
/// </summary>
public class HUD : MonoBehaviour
{
    [Tooltip("Reference to the HUD text that shows the player's current cash on hand")]
    public TextMeshProUGUI pocketChange;

    [Tooltip("Reference to the HUD text that shows the player's current debt")]
    public TextMeshProUGUI debtTracker;

    [Tooltip("Reference to the HUD object that functions as a compass toward the escape portal")]
    public GameObject compassImage;

    [Tooltip("Reference to the 2D GUI transform of the escape compass, so it can be rotated")]
    public RectTransform escapeCompass;

    [Tooltip("Reference to the money loss text object")]
    public TextMeshProUGUI shotLoss;

    [Tooltip("Reference to the money gain text object")]
    public TextMeshProUGUI earn;

    [Tooltip("Reference to the FPS text object")]
    public TextMeshProUGUI fps;

    /* Variables set at runtime */
    /// <summary>
    /// Reference the player's transform, so the player's position can be
    /// used to determine the compass bearing
    /// </summary>
    public Transform playerTransform { get; set; }

    /// <summary>
    /// Reference to the escape portal's transform, so the position can
    /// be used to determine the compass bearing
    /// </summary>
    public Transform escapeHatch { get; set; }

    /// <summary>
    /// Singleton management
    /// </summary>
    public static HUD hudInstance { get; private set; }

    /// <summary>
    /// How frequently, in seconds, the FPS text should update. Updating less
    /// frequently than each frame makes the text more readable, and also gives
    /// a more accurate-feeling value. The value is smoothed over time, so
    /// stutters of just a few frames don't adversely affect the display.
    /// </summary>
    private float fpsUpdateRate = 1f;  // in seconds

    /// <summary>
    /// Accumulated FPS values. Used to calculate the smoothed FPS over time
    /// sicne the last update.
    /// </summary>
    private float accumulatedFps;

    /// <summary>
    /// Ticks/frames since the last FPS update.
    /// </summary>
    private float ticksSinceLastUpdate;

    /// <summary>
    /// Number of ticks since the last FPS update.
    /// </summary>
    private int numTicksSinceLast;

    /// <summary>
    /// Singleton management.
    /// </summary>
    private void Awake() {
        if (hudInstance != null && hudInstance != this) {
            Destroy(this.gameObject);
            return;
        } else {
            hudInstance = this;
        }
        DontDestroyOnLoad(this.gameObject);
        StateManager.singletons.Add(this.gameObject);
    }

    /// <summary>
    /// Initialize values and references used by the HUD.
    /// </summary>
    private void Start() {
        shotLoss.text = "";
        earn.text = "";
        fps.enabled = Settings.values.showFPS;
    }

    /// <summary>
    /// Processes updates to the HUD every frame.
    /// </summary>
    private void Update() {
        pocketChange.text = $"{StateManager.cashOnHand.ToString("N2")}";
        debtTracker.text = $"DEBT: {(-StateManager.totalDebt).ToString("N2")}";

        // Handle FPS display when applicable
        if (Settings.values.showFPS && Time.timeScale > 0) {
            ticksSinceLastUpdate += Time.deltaTime;
            numTicksSinceLast++;
            accumulatedFps += Time.smoothDeltaTime;
            if (ticksSinceLastUpdate > fpsUpdateRate) {
                int fpsValue = (int)(1.0f/(accumulatedFps/numTicksSinceLast));
                // value can be negative shortly after unpausing, don't update it
                // and it also goes sky-high sometimes, let's try to not run the game at 1k fps
                if (fpsValue > 0 && fpsValue < 1000) {
                    fps.text = $"{fpsValue}";
                }
                ticksSinceLastUpdate = 0f;
                numTicksSinceLast = 0;
                accumulatedFps = 0;
            }
        }

        // Handle compass rotation
        if (escapeHatch != null && playerTransform != null) {
            compassImage.SetActive(true);
            Vector3 dir = playerTransform.position - escapeHatch.position;
            float playerHeading = playerTransform.rotation.eulerAngles.y;
            float angleToEscape = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float compassAngle = playerHeading - angleToEscape + 185;  // orig image does not point at 0 deg at z-rot 0, correction factor is 185
            escapeCompass.rotation = Quaternion.Slerp(escapeCompass.rotation, Quaternion.Euler(new Vector3(0, 0, compassAngle)), Time.deltaTime*10);
        } else {
            compassImage.SetActive(false);
        }
    }
}
