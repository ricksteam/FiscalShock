using UnityEngine;
using FiscalShock.Graphs;
using FiscalShock.Procedural;

namespace FiscalShock.Pathfinding {
    /// <summary>
    /// Class that describes an enemy movement state manager and controls
    /// the use of A star for pathfinding.
    /// </summary>
    public class Hivemind : MonoBehaviour {
        /// <summary>
        /// The last cell which the player was in. Set by player movement script
        /// on start up, and by the movement script after that.
        /// </summary>
        public Vertex lastPlayerLocation { get; set; }

        /// <summary>
        /// An instance of the A* pathfinder.
        /// </summary>
        public AStar pathfinder { get; private set; }

        /// <summary>
        /// Obtains the navigable Delaunay triangulation and instantiates the pathfinder
        /// with it.
        /// </summary>
        void Start() {
            Dungeoneer dungeoneer = GameObject.Find("DungeonSummoner").GetComponent<Dungeoneer>();
            Delaunay graph = dungeoneer.navigableDelaunay;

            pathfinder = new AStar(graph);
        }
    }
}
