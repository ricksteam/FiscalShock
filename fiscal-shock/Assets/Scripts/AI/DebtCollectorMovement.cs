using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using FiscalShock.Graphs;
using FiscalShock.Pathfinding;
using FiscalShock.Procedural;
using System.IO;
using System.Collections;

namespace FiscalShock.AI {
    /// <summary>
    /// A* search and localized pathfinding that includes wall avoidance and
    /// the ability to climb vertical faces of objects for the Debt Collector.
    /// </summary>
    public class DebtCollectorMovement : MonoBehaviour {
        [Tooltip("The speed at which the object moves.")]
        public float movementSpeed = 3f;

        [Tooltip("The speed at which the object turns.")]
        public float rotationSpeed = 7f;

        [Tooltip("The absolute minimum distance away from the player.")]
        public float safeRadiusMin = 4f;

        [Tooltip("Creates safe radius in case object ends up too close to player.")]
        public float safeRadiusMax = 5f;

        [Tooltip("How close the player needs to be before being pursued.")]
        public float visionRadius = 35f;

        /// <summary>
        /// Player object.
        /// </summary>
        public GameObject player;

        /// <summary>
        /// Whether or not the debt collector has been stunned.
        /// </summary>
        public bool stunned { get; set; }

        /// <summary>
        /// Distance from the player in 2D space (x,z).
        /// </summary>
        public float distanceFromPlayer2D { get; private set; }

        /// <summary>
        /// Controller used to move the debt collector.
        /// </summary>
        private CharacterController controller;

        // Pathfinding

        /// <summary>
        /// The point at which the player originally spawned.
        /// </summary>
        public Vertex spawnPoint { get; set; }

        /// <summary>
        /// The Delaunay vertex of the last trigger zone that the debt collector entered.
        /// </summary>
        internal Vertex lastVisitedNode { get; set; } = null;

        /// <summary>
        /// The vertex corresponding to the pathfinding destination.
        /// </summary>
        private Vertex nextDestinationNode = null;

        /// <summary>
        /// Reference to the enemy movement state manager.
        /// </summary>
        private Hivemind hivemind;

        /// <summary>
        /// Reference to the enemy controlling script that contains the A*.
        /// Useful if wanted to expand A* to other enemy characters.
        /// </summary>
        private AStar pathfinder;

        /// <summary>
        /// The path returned from pathfinding.
        /// </summary>
        private Stack<Vertex> path;

        /// <summary>
        /// The pathfinding destination.
        /// </summary>
        private Vector3 nextDestination;

        /// <summary>
        /// The direction of the next destination point in 2D space, transposed to 3D.
        /// </summary>
        private Vector3 nextFlatDir;

        /// <summary>
        /// The amount of updates that have passed since the last path recalculation.
        /// </summary>
        private int recalculationCount = -1;

        /// <summary>
        /// The number of updates necessary to recalculate the path.
        /// </summary>
        private int recalculationRate = 500;

        // Raycasting

        /// <summary>
        /// The 0 degree whisker for raycasting.
        /// </summary>
        private Vector3 forwardWhisker;

        /// <summary>
        /// The -150, -120, -75, 75, 120, 150, and 180 degree whiskers for raycasting.
        /// </summary>
        private Vector3 left75, right75, left120, right120, left150, right150, backward;

        /// <summary>
        /// How long the whiskers should be.
        /// </summary>
        private float whiskerLength = 5f;

        /// <summary>
        /// The amount of updates that should pass before the debt collector is teleported
        /// to a new location, probably due to being stuck in a corner.
        /// </summary>
        private int teleportationSaveRate =  300;

        /// <summary>
        /// The amount of time that has passed since the debt collector entered
        /// a new trigger zone.
        /// </summary>
        internal int saveCounter = 0;

        /// <summary>
        /// The last few nodes the DC has visited. Used to prevent getting stuck
        /// running around the same few cells.
        /// </summary>
        public List<Vertex> recentlyVisitedNodes = new List<Vertex>();

        /// <summary>
        /// How high up the debt collector should be teleported. Needed because
        /// teleporting on top of obstacles.
        /// </summary>
        private float teleportationHeight;

        /// <summary>
        /// The closest the DC can be to the player and still teleport
        /// to correct "stuck" pathfinding. The player might be kiting or
        /// otherwise keeping the DC "stuck" in the same few cells, but
        /// that doesn't mean we should teleport.
        /// </summary>
        private float minDistanceFromPlayerToTeleport = 32f;

        /// <summary>
        /// Layers the debt collector can climb/jump over.
        /// </summary>
        private LayerMask jumpable;

        /// <summary>
        /// Layers the debt collector tries to avoid.
        /// </summary>
        private LayerMask avoidance;

        private float damageTaken = 0;

        /// <summary>
        /// Modifier on the base stun threshold calculation. Based on floors
        /// visited, so it gets harder to stun him the more you've been around.
        /// </summary>
        private float stunThresholdModifier;

        /// <summary>
        /// Base threshold for stunning the debt collector. Based on how much
        /// debt you owe to any lender.
        /// </summary>
        private float stunThreshold;

        [Tooltip("The stun effect game object attached to this prefab.")]
        public GameObject stunEffect;

        /// <summary>
        /// Speed modifier. Debt collector is faster when you're more indebted.
        /// </summary>
        private float debtSpeedMod = (float)Mathf.Log10(Mathf.Pow(StateManager.totalDebt, 0.45f));

        [Tooltip("Current forward direction vector. Visible for debugging purposes.")]
        public Vector3 fwd;

        [Header("Climbing/Jumping/Vertical Obstacle Avoidance")]
        [Tooltip("Current vertical speed.")]
        public float verticalSpeed = 0;

        [Tooltip("Gravity modifier. More gravity makes debt collector fall faster.")]
        public float gravity = 20f;

        [Tooltip("Whether the debt collector should jump at the next update.")]
        public bool startJumping = false;

        /// <summary>
        /// Whether the debt collector is currently airborne. Enemies
        /// are typically spawned in the air, and the debt collector
        /// will correct itself if it spawned on solid ground during
        /// the next update anyway.
        /// </summary>
        private bool airborne = true;

        [Tooltip("Maximum jump height. A shorter jump height results in the debt collector appearing to 'climb' objects. A jump height too short results in the debt collector being unable to climb objects over a certain height.")]
        public float jumpHeight = 2f;

        /// <summary>
        /// Radius of the box used in the box check when the debt collector
        /// tries to scale a wall. Should be equal or very close to the extents
        /// of the character controller.
        /// </summary>
        private float footSize;

        /// <summary>
        /// Initializes the necessary fields for the debt collector and
        /// initializes the AStar script.
        /// </summary>
        private void Start() {
            if (player == null) {
                player = GameObject.FindGameObjectWithTag("Player");
            }

            controller = GetComponentInParent<CharacterController>();

            GameObject dungeonMaster = GameObject.Find("DungeonSummoner");

            hivemind = dungeonMaster.GetComponent<Hivemind>();
            pathfinder = hivemind.pathfinder;
            lastVisitedNode = spawnPoint;
            teleportationHeight = dungeonMaster.GetComponent<Dungeoneer>().currentDungeonType.wallHeight * 0.8f;

            // LayerMask.NameToLayer can only be used at runtime.
            jumpable = ((1 << LayerMask.NameToLayer("Obstacle")) | (1 << LayerMask.NameToLayer("Explosive") | (1 << LayerMask.NameToLayer("Decoration"))));
            avoidance = (1 << LayerMask.NameToLayer("Wall"));

            // Set stun thresholds.
            stunThresholdModifier = (float)Mathf.Log10(Mathf.Pow(StateManager.totalFloorsVisited, 0.3f)) + StateManager.totalFloorsVisited/4.0f + 1.0f;
            stunThreshold = ((float)Mathf.Pow(StateManager.totalDebt, 1.7f) / 3333.0f + 15) * stunThresholdModifier;

            // Set speed.
            if (float.IsNaN(debtSpeedMod) || float.IsInfinity(debtSpeedMod)) {
                debtSpeedMod = 1;
            }
            float playerSpeed = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().speed;
            debtSpeedMod = Mathf.Clamp(debtSpeedMod, 1, Mathf.Ceil(playerSpeed/movementSpeed + 0.5f));
            movementSpeed *= debtSpeedMod;

            // Determine extents of the box check for climbing/jumping
            footSize = controller.bounds.extents.y * 1.1f;

            // DEBUG
            #if UNITY_EDITOR
            //Debug.Log("LAST VISITED NODE: " + lastVisitedNode.vector);
            Debug.Log($"DC speed is {movementSpeed} and x{debtSpeedMod}; player speed is {playerSpeed}");
            #endif
        }

        /// <summary>
        /// Detect any obstacles and determine the best direction to go towards.
        /// Opening the Unity Editor scene view and watching the DC is the
        /// quickest way to see what's going on here.
        /// </summary>
        /// <param name="target">The direction against which to raycast.</param>
        /// <returns>A vector representing the safest direction to go towards.</returns>
        private Vector3 findSafeDirection(Vector3 target) {
            forwardWhisker = target;

            // Draw the forward whisker.
            #if UNITY_EDITOR
            Debug.DrawRay(transform.position, forwardWhisker * whiskerLength, Color.blue, 2);
            #endif

            // Create right 75 degrees and left 75 degrees whiskers.
            left75 = Quaternion.Euler(0, -75, 0) * forwardWhisker;
            right75 = Quaternion.Euler(0, 75, 0) * forwardWhisker;

            RaycastHit hit;
            // Check the forward whisker.
            if (Physics.Raycast(transform.position, forwardWhisker, out hit, whiskerLength, avoidance)) {
                // Debug.Log($"Forward whisker hit {hit.collider.gameObject.name}");

                // Draw the left and right whiskers.
                #if UNITY_EDITOR
                Debug.DrawRay(transform.position, right75 * whiskerLength, Color.red, 1);
                Debug.DrawRay(transform.position, left75 * whiskerLength, Color.green, 1);
                #endif

                // Find out if the 75 degree left and right whiskers hits something.
                bool hitLeft, hitRight;
                hitLeft = Physics.Raycast(transform.position, left75, whiskerLength, avoidance);
                hitRight = Physics.Raycast(transform.position, right75, whiskerLength, avoidance);

                // Determine which whiskers were hit;
                if (hitLeft && !hitRight) {
                    return right75;
                }

                else if (!hitLeft && hitRight) {
                    return left75;
                }

                // Empty left and right to reuse for the next set of whiskers.
                else if (hitLeft && hitRight) {
                    hitLeft = false;
                    hitRight = false;
                }

                // Neither left nor right whisker hit.
                else {
                    return left75;
                }

                // If didn't return in one of the previous cases, good sign that need to check next whiskers.
                // Calculate 120 degree left and right whiskers.
                left120 = Quaternion.Euler(0, -120, 0) * forwardWhisker;
                right120 = Quaternion.Euler(0, 120, 0) * forwardWhisker;

                // Draw the whiskers.
                #if UNITY_EDITOR
                Debug.DrawRay(transform.position, right120 * whiskerLength, Color.gray, 1);
                Debug.DrawRay(transform.position, left120 * whiskerLength, Color.yellow, 1);
                #endif

                // Find out if the 120 degree left and right whiskers hit something.
                hitLeft = Physics.Raycast(transform.position, left120, whiskerLength, avoidance);
                hitRight = Physics.Raycast(transform.position, right120, whiskerLength, avoidance);

                // Determine which whiskers were hit;
                if (hitLeft && !hitRight) {
                    return right120;
                }

                else if (!hitLeft && hitRight) {
                    return left120;
                }

                // Empty left and right to reuse for the next set of whiskers.
                else if (hitLeft && hitRight) {
                    hitLeft = false;
                    hitRight = false;
                }

                // Neither left nor right whisker hit.
                else {
                    return left120;
                }

                // If didn't return in one of the previous cases, good sign that need to check next whiskers.
                // Calculate 150 degree left and right whiskers.
                left150 = Quaternion.Euler(0, -150, 0) * forwardWhisker;
                right150 = Quaternion.Euler(0, 150, 0) * forwardWhisker;

                // Draw the whiskers.
                #if UNITY_EDITOR
                Debug.DrawRay(transform.position, right150 * whiskerLength, Color.magenta, 1);
                Debug.DrawRay(transform.position, left150 * whiskerLength, Color.white, 1);
                #endif

                // Find out if the 120 degree left and right whiskers hit something.
                hitLeft = Physics.Raycast(transform.position, left150, whiskerLength, avoidance);
                hitRight = Physics.Raycast(transform.position, right150, whiskerLength, avoidance);

                // Determine which whiskers were hit;
                if (hitLeft && !hitRight) {
                    return right150;
                }

                else if (!hitLeft && hitRight) {
                    return left150;
                }

                // Empty left and right to reuse for the next set of whiskers.
                else if (hitLeft && hitRight) {
                    hitLeft = false;
                    hitRight = false;
                }

                // Neither left nor right whisker hit.
                else {
                    return left150;
                }

                // If absolutely no other case works, return the backwards angle.
                backward = Vector3.Reflect(forwardWhisker * whiskerLength, hit.normal);

                // Draw the backwards ray.
                #if UNITY_EDITOR
                Debug.DrawRay(transform.position, backward * whiskerLength, Color.cyan, 1);
                #endif

                return backward;
            }

            return forwardWhisker;
        }

        /// <summary>
        /// Updates pathfinding every fixed update (currently 0.02 in project
        /// settings).
        /// </summary>
        private void FixedUpdate() {
            // Can be stunned, but not hurt. He is immortal. Only death can free you of debt.
            // (Or, ya' know, paying off your debt.)
            if (player == null || stunned) {
                return;
            }

            // Figure out the vectors for where to head to.
            Vector3 playerDirection = (player.transform.position - transform.position).normalized;
            Vector3 flatPlayerDirection = new Vector3(playerDirection.x, 0, playerDirection.z);
            Vector2 flatPosition = new Vector2(transform.position.x, transform.position.z);
            Vector2 playerFlatPosition = new Vector2(player.transform.position.x, player.transform.position.z);

            distanceFromPlayer2D = Vector2.Distance(playerFlatPosition, flatPosition);

            saveCounter++;

            // Apply gravity as needed.
            setVerticalMovement();

            // DC has been in the same cell for too long
            // But we don't want to teleport when we're really close to the player
            if (saveCounter >= teleportationSaveRate && distanceFromPlayer2D > minDistanceFromPlayerToTeleport) {
                // Easy to determine teleportation destination when the path is filled.
                if (path != null && path.Count > 0) {
                    if (nextDestinationNode == null) {
                        // Grab the next destination from the path.
                        nextDestinationNode = path.Pop();
                    }

                    // Turn off the character controller.
                    controller.enabled = false;

                    // Teleport to the nextDestinationNode (at 80% of max height).
                    verticalSpeed = 0f;
                    airborne = true;
                    transform.position = new Vector3(nextDestinationNode.x, teleportationHeight, nextDestinationNode.y);

                    // Turn on the character controller again.
                    controller.enabled = true;

                    // If don't reset this value, gets stuck because never entered via the colliders.
                    lastVisitedNode = nextDestinationNode;

                    // Set a new destination or prepare to recalculate the path.
                    if (path.Count > 0) {
                        nextDestinationNode = path.Pop();
                    }

                    else {
                        path = null;
                        recalculationCount = 0;
                    }
                }

                else {
                    // Determine a valid cell to transport to.
                    Vertex teleportTo = lastVisitedNode.cell.neighbors.First(c => c.reachable).site;

                    // Move the debt collector to the location.
                    controller.enabled = false;
                    transform.position = new Vector3(teleportTo.x, teleportationHeight, teleportTo.y);
                    controller.enabled = true;
                    lastVisitedNode = teleportTo;

                    // In case the path count was 0 but the path wasn't yet set to null.
                    if (path != null) {
                        path = null;
                        recalculationCount = 0;
                    }
                }

                // Now at a new location, so can reset the counter.
                saveCounter = 0;
                return;
            }

            // Straight line pursuit. Want to catch player, so no retreat.
            if (distanceFromPlayer2D < visionRadius) {
                // Unlikely that the path will be valid if player gets away.
                if (path != null) {
                    path = null;
                    recalculationCount = 0;
                }

                // Obtain the "safe" direction to go.
                Vector3 safeDir = findSafeDirection(flatPlayerDirection);

                // Draw the direction to the player.
                #if UNITY_EDITOR
                Debug.DrawRay(transform.position, safeDir * whiskerLength, Color.black, 1);
                #endif

                applyMovement(safeDir);
                return;
            }

            if (path == null) {
                // Because script execution order apparently means nothing now.
                if (hivemind.lastPlayerLocation == null) {
                    return;
                }

                // DEBUG: Remove.
                #if UNITY_EDITOR
                //Debug.Log("RECALCULATING PATH.");
                #endif
                path = pathfinder.findPath(lastVisitedNode, hivemind.lastPlayerLocation);

                // Was extremely close to destination. A path was not needed.
                if (path.Count == 0) {
                    path = null;
                    return;
                }

                // DEBUG: Uncomment if behavior is unexpected.
                // outputPathToFile();

                // Start navigating path by obtaining first vertex.
                nextDestinationNode = path.Pop();

                // DEBUG: Remove or set debugging code.
                #if UNITY_EDITOR
                //Debug.Log("NEXT DESTINATION: " + nextDestinationNode.vector);
                #endif

                nextDestination = new Vector3(nextDestinationNode.x, transform.position.y, nextDestinationNode.y);
                Vector3 unnormDirection = nextDestination - transform.position;
                nextFlatDir = new Vector3(unnormDirection.x, 0, unnormDirection.z).normalized;

                return;
            }

            // Prepare for recalculation of path.
            if (recalculationCount >= recalculationRate) {
                path = null;
                recalculationCount = 0;
            }

            // Obtain next node or prepare for recalculation if triggered zone site and destination are equal.
            if (lastVisitedNode.Equals(nextDestinationNode)) {
                // Deal with unknown null path.
                if (path == null) {
                    return;
                }

                if (path.Count > 0) {
                    nextDestinationNode = path.Pop();

                    // DEBUG: Remove or set debugging code.
                    #if UNITY_EDITOR
                    //Debug.Log("NEXT DESTINATION: " + nextDestinationNode.vector);
                    #endif

                    nextDestination = new Vector3(nextDestinationNode.x, transform.position.y, nextDestinationNode.y);
                    Vector3 unnormDirection = nextDestination - transform.position;
                    nextFlatDir = new Vector3(unnormDirection.x, 0, unnormDirection.z).normalized;

                    Vector3 safeDir = findSafeDirection(nextFlatDir);

                    // Move in the safe direction.
                    applyMovement(safeDir);

                    // Because used pathfinding, must get closer to recalculation.
                    recalculationCount++;
                    return;
                }

                // Want to recalculate the path, as already reached our destination.
                path = null;
                recalculationCount = 0;
                return;
            }

            // Find the safe direction and move there.
            Vector3 safeDirection = findSafeDirection(nextFlatDir);
            applyMovement(safeDirection);
            recalculationCount++;
        }

        /// <summary>
        /// Apply previously calculated velocity changes.
        /// </summary>
        /// <param name="direction"><b>normalized</b> direction to move in</param>
        private void applyMovement(Vector3 direction) {
            // Rotate towards the desired direction.
            Quaternion safeDirectionRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, safeDirectionRotation, Time.deltaTime * rotationSpeed);

            fwd = transform.forward * movementSpeed;
            fwd.y = verticalSpeed;
            controller.Move(fwd * Time.deltaTime);
        }

        /// <summary>
        /// Apply velocity changes related to vertical movement.
        /// </summary>
        private void setVerticalMovement() {
            if (startJumping) {
                verticalSpeed = jumpHeight;
                startJumping = false;
                airborne = true;
                return;
            }

            // Check if we're (reasoanbly close to) touching the ground. If not, we're obviously airborne and need to apply gravity.
            if (Physics.Raycast(transform.position, -Vector3.up, footSize, (1 << LayerMask.NameToLayer("Ground") | avoidance))) {
                verticalSpeed = 0;
                airborne = false;
            } else if (airborne) {
                verticalSpeed -= gravity * Time.deltaTime;
            } else {
                airborne = true;
            }
        }

        /// <summary>
        /// Check for collisions. The controller collider was chosen, as it
        /// worked the best out of various colliders we experimented with.
        /// Unity's physics engine is not the nicest to deal with.
        /// <para>Because this is based on controller collisions, collisions
        /// are only detected from the front! That's okay, because the DC is
        /// always facing the player, except when stunned.</para>
        /// </summary>
        /// <param name="col">information on the hit that occurred</param>
        private void OnControllerColliderHit(ControllerColliderHit col) {
            if (stunned) {  // Can stun and touch without game over, to some extent
                return;
            }

            // Handle being shot
            if (col.gameObject.tag == "Missile" || col.gameObject.tag == "Bullet") {
                BulletBehavior bb = col.gameObject.GetComponent<BulletBehavior>();
                if (bb == null) {
                    damageTaken += 1.0f;
                    return;
                }
                damageTaken += bb.damage;
                if (damageTaken >= (stunThreshold * Random.Range(0.85f, 1.15f))) {
                    StartCoroutine(stun(Random.Range(3f, 5f)));
                }
            }

            // Handle touching the player by calling game over
            if (col.gameObject.tag == "Player" && !StateManager.playerDead && StateManager.totalDebt > 0) {
                player.GetComponent<PlayerHealth>().endGameByDebtCollector();
                return;
            }

            // Jump on lateral collisions. The DC still has some problems climbing certain objects, like the drums on their sides in the mines. Adjusting the value col.normal.y is checked against would help this.
            if (Mathf.Abs(col.normal.y) > 0.5f) {
                return;
            }
            if (Physics.SphereCast(transform.position, controller.bounds.extents.x, transform.forward, out RaycastHit _, 1f, jumpable)) {
                startJumping = true;
            }
        }

        /// <summary>
        /// Temporarily disables the debt collector for `duration`
        /// seconds and plays the stun effect animation. Also dims
        /// its color for more visual feedback.
        /// </summary>
        /// <param name="duration">stun duration in seconds</param>
        private IEnumerator stun(float duration) {
            stunned = true;
            Material mat = gameObject.GetComponentInChildren<Renderer>().material;
            mat.SetColor("_Color", new Color(0.1f, 0.1f, 0.1f));
            stunEffect.SetActive(true);
            yield return new WaitForSeconds(duration);

            damageTaken = 0;
            stunned = false;
            stunEffect.SetActive(false);
            mat.SetColor("_Color", Color.white);
            yield return null;
        }

        /// <summary>
        /// Interface to the internal stun function. External callers,
        /// like exploding barrels, might be destroyed before the stun
        /// duration ends. Destroyed objects never finish their coroutines.
        /// </summary>
        /// <param name="duration">stun duration in seconds</param>
        public void externalStun(float duration) {
            StartCoroutine(stun(duration));
        }

        /// <summary>
        /// Method that outputs the vertices of the path into a text file.
        /// </summary>
        private void outputPathToFile() {
            StreamWriter writer = new StreamWriter(string.Format("{0}/path.txt", Directory.GetCurrentDirectory()));
            Vertex[] pathNodes = path.ToArray();

            foreach (Vertex node in pathNodes) {
                writer.Write(node.vector + "\n");
            }

            writer.Close();
        }
    }
}
