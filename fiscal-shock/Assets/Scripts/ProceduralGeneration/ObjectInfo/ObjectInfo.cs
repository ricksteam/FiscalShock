using UnityEngine;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Intended to prevent spawning of objects that clip into
    /// each other by destroying one of them when a collision is
    /// detected.
    /// </summary>
    public class ObjectInfo : MonoBehaviour {
        public bool toBeDestroyed { get; set; }
        private float timer;
        private Rigidbody rb;

        private void Start() {
            // Trigger zones only work with rigid bodies
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        /// <summary>
        /// Prevent objects from intersecting with each other.
        /// Requires trigger colliders set on all objects to be destroyed.
        /// </summary>
        /// <param name="col"></param>
        private void OnTriggerEnter(Collider col) {
            switch (col.gameObject.tag) {
                case "Decoration":
                case "Obstacle":
                case "Portal":
                case "Wall":
                    ObjectInfo oi = col.gameObject.GetComponent<ObjectInfo>();
                    if (oi != null) {
                        oi.toBeDestroyed = true;
                    } else {
                        toBeDestroyed = true;
                    }
                    break;

                default:
                    break;
            }
        }

        private void FixedUpdate() {
            if (timer > 5) {
                this.enabled = false;
                Destroy(rb);
            }
            if (toBeDestroyed) {
                Destroy(gameObject);
            }

            timer += Time.deltaTime;
        }
    }
}
