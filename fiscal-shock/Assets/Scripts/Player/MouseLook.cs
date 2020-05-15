using UnityEngine;

/// <summary>
/// FPS camera, controlled by moving the mouse.
/// </summary>
public class MouseLook : MonoBehaviour
{
    public bool lockCursorToGame = true;
    public float clampMinimum = -90f;
    public float clampMaximum = 90f;

    public Transform body;

    private float xRotation = 0f;
    private CursorLockMode lastCursorLockState;

    private void Start() {
        Settings.lockCursorState(this);
    }

    /// <summary>
    /// Moves the camera with the mouse, uses Time.deltaTime for FPS correction (Independent of current Frame rate)
    /// </summary>
    private void Update() {
        float mouseX = Input.GetAxis("Mouse X") * Settings.mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * Settings.mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;

        //Cannot look further than 90 degrees up
        xRotation = Mathf.Clamp(xRotation, clampMinimum, clampMaximum);

        transform.localRotation = Quaternion.Euler(xRotation,0f,0f);
        body.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// Release cursor and freeze time when the game is no longer in
    /// focus.
    /// </summary>
    /// <param name="focused">whether the game window is now in focus</param>
    private void OnApplicationFocus(bool focused) {
        if (!focused) {  // game window is no longer in focus
            lastCursorLockState = Cursor.lockState;
            Settings.forceUnlockCursorState();
        } else if (lastCursorLockState == CursorLockMode.Locked) {
            // It used to be locked, lock it again
            Settings.forceLockCursorState();
        }
    }
}
