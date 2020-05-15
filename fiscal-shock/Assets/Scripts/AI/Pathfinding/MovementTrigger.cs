using UnityEngine;
using FiscalShock.Graphs;
using FiscalShock.AI;

namespace FiscalShock.Pathfinding {
    /// <summary>
    /// Class describing a trigger zone associated with a cell.
    /// </summary>
    public class MovementTrigger : MonoBehaviour {
        /// <summary>
        /// The cell vertex with which this trigger is associated.
        /// </summary>
        public Vertex cellSite { get; set; }

        /// <summary>
        /// Used to set the last visited location of the player.
        /// </summary>
        private Hivemind hivemind;

        /// <summary>
        /// Used to set the last visited location of the debt collector.
        /// </summary>
        private DebtCollectorMovement dcMovement;

        /// <summary>
        /// Hard cap on how many cells to remember visiting lately.
        /// Helps prevent getting stuck doing figure-eights across
        /// two different cells because you can't walk through walls.
        /// </summary>
        private readonly int maxCellsVisited = 3;

        private void Start() {
            hivemind = GameObject.Find("DungeonSummoner").GetComponent<Hivemind>();
        }

        private void OnTriggerEnter(Collider col) {
            if (col.gameObject.layer == 11) {
                // Debug.Log($"Player stepped into {gameObject.name}");
                hivemind.lastPlayerLocation = cellSite;
            }

            if (col.gameObject.layer == 16) {
                if (dcMovement == null) {
                    dcMovement = col.gameObject.GetComponentInChildren<DebtCollectorMovement>();
                }

                // Debug.Log($"Debt Collector stepped into {gameObject.name}");
                dcMovement.lastVisitedNode = cellSite;
                // If the debt collector already visited this site, he's possibly stuck.
                if (dcMovement.recentlyVisitedNodes.Contains(cellSite)) {
                    dcMovement.saveCounter++;
                    return;
                }
                // Otherwise, add it to the list of recently visited nodes.
                if (dcMovement.recentlyVisitedNodes.Count >= maxCellsVisited) {
                    dcMovement.recentlyVisitedNodes.RemoveAt(0);
                }
                dcMovement.recentlyVisitedNodes.Add(cellSite);
                dcMovement.saveCounter = 0;
            }
        }
    }
}
