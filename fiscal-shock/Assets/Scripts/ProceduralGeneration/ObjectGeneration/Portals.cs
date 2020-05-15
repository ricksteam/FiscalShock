using UnityEngine;
using FiscalShock.Graphs;
using System;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Spawns the escape and delve portals as applicable.
    /// </summary>
    public static class Portals {
        /// <summary>
        /// Retry the spawning if there is a collision with these layer.
        /// </summary>
        /// <returns></returns>
        private static readonly int layersToAvoid = (1 << 12) | (1 << 15) | (1 << 9);

        /// <summary>
        /// Place the delve portal at a randomized spot.
        /// This function should be called before `makeEscapePoint`!
        /// There is no check that a portal isn't spawned on another portal,
        /// and any object that already existed on the point is removed.
        /// </summary>
        /// <param name="d"></param>
        public static void makeDelvePoint(Dungeoneer d) {
            Debug.Log("Placing Delve portal");
            int delveSite = makePortal(d, d.currentDungeonType.delvePrefab);
            d.validCells[delveSite].spawnedObject.name = "Delve Point";
        }

        /// <summary>
        /// Place the escape portal at a randomized spot.
        /// This function should be called after `makeDelvePoint`!
        /// If it is not called last, the delve portal could destroy this portal
        /// </summary>
        /// <param name="d"></param>
        public static void makeEscapePoint(Dungeoneer d) {
            Debug.Log("Placing Escape portal");
            int escapeSite = makePortal(d, d.currentDungeonType.returnPrefab);
            d.validCells[escapeSite].spawnedObject.name = "Escape Point";
        }

        /// <summary>
        /// Spawns a particular portal. Removes objects that were already on
        /// that spot, rather than trying to find a new place.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="portal"></param>
        /// <returns></returns>
        public static int makePortal(Dungeoneer d, GameObject portal) {
            int portalSite = d.mt.Next(d.validCells.Count-1);
            Cell chosenCell = d.validCells[portalSite];

            // Remove any currently spawned objects here
            if (chosenCell.spawnedObject != null) {
                UnityEngine.Object.Destroy(chosenCell.spawnedObject);
            }

            Vector3 where = new Vector3(chosenCell.site.x, 0, chosenCell.site.y);
            chosenCell.spawnedObject = UnityEngine.Object.Instantiate(portal, where, portal.transform.rotation);
            // Randomly rotate about the y-axis
            float rotation = d.mt.Next(360);
            chosenCell.spawnedObject.transform.Rotate(0, rotation, 0);
            chosenCell.hasPortal = true;

            Bounds bounds = chosenCell.spawnedObject.GetComponentInChildren<Renderer>().bounds;

            // Try to respawn it if it's clipping into something, usually a wall
            Collider[] wtf = Physics.OverlapBox(where, bounds.extents, chosenCell.spawnedObject.transform.rotation, layersToAvoid);
            foreach (Collider col in wtf) {
                if (col.gameObject.layer == LayerMask.NameToLayer("Wall")) {
                    Debug.Log($"Need to retry portal: object was {col.gameObject} on layer {LayerMask.LayerToName(col.gameObject.layer)}");
                    return makePortal(d, portal);
                }
                ObjectInfo oi = col.gameObject.GetComponent<ObjectInfo>();
                if (oi != null) {
                    oi.toBeDestroyed = true;
                }
            }

            return portalSite;
        }
    }
}
