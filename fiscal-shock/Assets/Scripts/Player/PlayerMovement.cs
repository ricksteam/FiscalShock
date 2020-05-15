using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FiscalShock.Graphs;
using FiscalShock.Pathfinding;

/// <summary>
/// Translates user input into player movement (jumping and running).
/// References:
/// Player Movement: https://www.youtube.com/watch?v=_QajrabyTJc&amp;t=1s
/// Character Controller documentation: https://docs.unity3d.com/Manual/class-CharacterController.html
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Reference to the player's character controller")]
    public CharacterController controller;

    [Tooltip("Ground movement speed of the player.")]
    public float speed = 12f;

    [Tooltip("Gravity value, used to improve jump physics.")]
    public float gravity = -50f;

    [Tooltip("Jump height.")]
    public float jumpBy = 1.5f;

    [Tooltip("Reference to a ground check object ('feet') that helps determine whether the player is on solid ground.")]
    public Transform groundCheck;

    [Tooltip("Maximum allowable distance from the ground object to solid ground (in any direction).")]
    public float groundDistance = 0.4f;

    [Tooltip("Current velocity of the player.")]
    public Vector3 velocity;

    /// <summary>
    /// Whether the player is currently on solid ground.
    /// </summary>
    private bool isGrounded;

    [Tooltip("Reference to an input action object from Unity's new input system.")]
    public InputActionAsset inputActions;

    [Header("Grounded (Jump-Enabled) Layers")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public LayerMask decorationMask;

    /// <summary>
    /// The Delaunay triangulation vertex where the player originally spawned.
    /// </summary>
    internal Vertex originalSpawn { get; set; }

    /// <summary>
    /// 2D movement based on input events
    /// </summary>
    private Vector2 movement;

    /// <summary>
    /// Whether the player is currently jumping
    /// </summary>
    private bool jumping;

    /// <summary>
    /// Assigns the input system configuration to the player's input controller.
    /// The new input system is currently only used for jumping. 2D axis
    /// movement with the new system handles slightly differently and was not
    /// implemented fully.
    /// </summary>
    private void Awake() {
        gameObject.GetComponent<PlayerInput>().actions = inputActions;
    }

    /// <summary>
    /// Assigns the initial player location value in the hivemind.
    /// </summary>
    private void Start() {
        // TODO: This should really be in Hivemind's start, but we ended up not using Hivemind
        GameObject dungeoneer = GameObject.Find("DungeonSummoner");
        if (dungeoneer != null) {
            Hivemind hivemind = dungeoneer.GetComponent<Hivemind>();
            hivemind.lastPlayerLocation = originalSpawn;
        }
        // Debug.Log(hivemind.lastPlayerLocation.vector);
    }

    /// <summary>
    /// Updates the 2D movement vector, based on the new input system event
    /// handling.
    /// This value is not currently used for ground movement.
    /// </summary>
    /// <param name="cont">context</param>
    public void OnMovement(InputAction.CallbackContext cont) {
        movement = cont.ReadValue<Vector2>();
    }

    /// <summary>
    /// Updates whether the player is currently jumping, based on the new input
    /// system event handling.
    /// This value is used to allow the player to jump.
    /// </summary>
    /// <param name="cont"></param>
    /// <returns></returns>
    public void OnJump(InputAction.CallbackContext cont) {
        jumping = cont.phase == InputActionPhase.Performed;
    }

    /// <summary>
    /// Process inputs or states updated by input system event handling.
    /// </summary>
    private void Update()
    {
        // Creates sphere around object to check if it has collided with a ground layer
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance,groundMask | obstacleMask | decorationMask);

        // Resets velocity, so it doesn't go down forever
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Old Unity input system: get the 2D movement values
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate direction vector based on user input and current facing
        Vector3 move = transform.right * x + transform.forward * z;

        // Move the player character controller, adjusting for movement speed and time since the last Update()
        controller.Move(move * speed * Time.deltaTime);

        // Only Jump if player is grounded, velocity.y brings player down
        if(jumping && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpBy * -2f * gravity);
        }

        // Velocity for when user is falling down, using gravity and Time.deltaTime.
        velocity.y += gravity * Time.deltaTime;

        // Apply downward gravity to keep the player from flying away.
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Teleports the player to a specific destination. The character controller
    /// must be disabled to move the player transform, or else the player
    /// will snap back into place instantly.
    /// </summary>
    /// <param name="destination">world space position to teleport to</param>
    /// <returns></returns>
    public void teleport(Vector3 destination) {
        controller.enabled = false;
        transform.position = destination;
        controller.enabled = true;
        Debug.Log($"Teleported to {destination}");
    }
}
