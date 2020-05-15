using UnityEngine;
using FiscalShock.Graphs;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Information a wall gameobject needs to know for dungeon generation
    /// and wall destruction activities.
    /// </summary>
    public class WallInfo : MonoBehaviour {
        /// <summary>
        /// Edge associated with this wall
        /// </summary>
        public Edge associatedEdge { get; set; }

        private Cheats cheater;

        private void Start() {
            cheater = GameObject.Find("Cheater").GetComponent<Cheats>();
        }

        /// <summary>
        /// Cheat for destroying walls
        /// </summary>
        /// <param name="col">collision event</param>
        private void OnCollisionEnter(Collision col) {
            if (col.gameObject == null) {
                return;
            }
            if (cheater.destroyWalls && (col.gameObject.tag == "Bullet" || col.gameObject.tag == "Missile")) {
                Destroy(gameObject);
            }
        }
    }
}
