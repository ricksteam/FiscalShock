using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using FiscalShock.Graphs;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Handles generation of walls in a dungeon and unfortunately some other
    /// miscellaneous tasks.
    /// </summary>
    public static class Walls {
        /// <summary>
        /// All CharacterControllers should be able to fit through hallways.
        /// Ideally, this would get each character controller from the
        /// DungeonType's list of enemies and find the max of the diameters.
        /// </summary>
        private readonly static float FATTEST_CONTROLLER = 6f;

        /// <summary>
        /// Main function to generate walls and update pathfinding graphs.
        /// The order of functions is important! Some variables are set in
        /// certain functions, and other conditions check for the existence
        /// of walls under the expectation that they are (or are not) there.
        /// </summary>
        /// <param name="d">dungeoneer object</param>
        /// <returns>list of cells that may be used for A* pathfinding</returns>
        public static List<Cell> setWalls(Dungeoneer d) {
            // Create walls everywhere, except for the rooms. This also
            // excludes the exteriors of the rooms.
            constructWallsOnVoronoi(d);

            // Create walls on the exteriors of the rooms.
            constructWallsOnRooms(d);

            // Eliminate walls where corridors should exist.
            List<GameObject> wallsToKeep = destroyWallsForCorridors(d);

            // Bounding box needs to be set up before reachableCells.
            // This function sets the min/max vectors on the dungeoneer
            // object that reachableCells needs to check.
            constructEnemyAvoidanceBoundingBox(d);

            // Enumerate reachable cells for A* pathfinding.
            // Note: Close-range pathfinding does not take reachable cells into
            // account.
            // A cell is only "A*-reachable" under the following conditions:
            // 1. At least one of its sides is not a wall
            // 2. None of its vertices exist outside the bounds of the walls
            // The bounds are tracked as 3D vertices, so the 2D cell vertex's y
            // must be compared to the 3D corner's z.
            List<Cell> reachableCells = d.vd.cells.Where(c => {
                c.reachable = (
                    !c.sides.All(s => s.isWall)
                    && !c.vertices.Any(v =>
                           v.x < d.bottomLeftWallCorner.x
                        || v.x > d.topRightWallCorner.x
                        || v.y < d.bottomLeftWallCorner.z
                        || v.y > d.topRightWallCorner.z)
                    );
                return c.reachable;
            }).ToList();

            // Extraneous walls must be destroyed *after* reachableCells are
            // determined. Otherwise, the cleared area is considered reachable.
            destroyLagWalls(d, wallsToKeep);

            return reachableCells;
        }

        /// <summary>
        /// Stand up trigger zones on the bounding box of the world for
        /// enemy movement AI to detect using raycasts. Also stands up
        /// walls to prevent the player from falling off of the world.
        /// </summary>
        private static void constructEnemyAvoidanceBoundingBox(Dungeoneer d) {
            GameObject trigger = GameObject.Find("EnemyAvoidanceTrigger");
            GameObject floor = GameObject.FindGameObjectWithTag("Ground");
            Bounds floorBounds = floor.GetComponent<Renderer>().bounds;

            Vector3 topLeft = new Vector3(0, 0, floorBounds.extents.z*2);
            Vector3 topRight = new Vector3(floorBounds.extents.x*2, 0, floorBounds.extents.z*2);
            Vector3 bottomLeft = new Vector3(0, 0, 0);

            // Update the dungeoneer object with info about the min/max reachable points. These points are implied by the bounds of the floor.
            d.topRightWallCorner = floorBounds.max;
            d.bottomLeftWallCorner = floorBounds.min;

            float xlen = Vector3.Distance(topLeft, topRight);
            float zlen = Vector3.Distance(topLeft, bottomLeft);

            Vector3 west = new Vector3(floorBounds.min.x, d.currentDungeonType.wallHeight/2, d.currentDungeonType.height/2);
            Vector3 north = new Vector3(d.currentDungeonType.width/2, d.currentDungeonType.wallHeight/2, floorBounds.max.z);
            Vector3 east = new Vector3(floorBounds.max.x, d.currentDungeonType.wallHeight/2, d.currentDungeonType.height/2);
            Vector3 south = new Vector3(d.currentDungeonType.width/2, d.currentDungeonType.wallHeight/2, floorBounds.min.z);

            setAvoidanceBoxOnSide(trigger, west, 1, d.currentDungeonType.wallHeight, zlen, d.currentDungeonType.wall.prefab);
            setAvoidanceBoxOnSide(trigger, north, xlen, d.currentDungeonType.wallHeight, 1, d.currentDungeonType.wall.prefab);
            setAvoidanceBoxOnSide(trigger, east, 1, d.currentDungeonType.wallHeight, zlen, d.currentDungeonType.wall.prefab);
            setAvoidanceBoxOnSide(trigger, south, xlen, d.currentDungeonType.wallHeight, 1, d.currentDungeonType.wall.prefab);

            // The original one isn't used, but it stays in the middle of the map, so destroy it to prevent weirdness on the AI
            UnityEngine.Object.Destroy(trigger.gameObject);
        }

        private static void setAvoidanceBoxOnSide(GameObject prefab, Vector3 position, float scaleX, float scaleY, float scaleZ, GameObject wall) {
            GameObject side = UnityEngine.Object.Instantiate(prefab, position, prefab.transform.rotation);
            side.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            side.transform.parent = prefab.transform.parent;
            GameObject sidewall = UnityEngine.Object.Instantiate(wall, side.transform);
            sidewall.transform.localScale = new Vector3(1, 1, 1);
            // Change the tag. Tag is used for destroying unnecessary walls.
            sidewall.tag = "Obstacle";
        }

        /// <summary>
        /// Generate walls all over the Voronoi, except for room interiors. Used
        /// to do subtractive corridor generation.
        /// </summary>
        /// <param name="d"></param>
        private static void constructWallsOnVoronoi(Dungeoneer d) {
            List<Cell> roomCells = d.roomVoronoi.SelectMany(r => r.cells).ToList();
            foreach (Cell c in d.vd.cells) {
                if (!roomCells.Contains(c)) {
                    constructWallsOnPolygon(d, c);
                }
            }
        }

        /// <summary>
        /// Make walls only on Voronoi rooms
        /// </summary>
        /// <param name="d"></param>
        private static void constructWallsOnRooms(Dungeoneer d) {
            foreach (VoronoiRoom r in d.roomVoronoi) {
                constructWallsOnPolygon(d, r.exterior);
            }
        }

        /// <summary>
        /// Make walls on all edges of a polygon
        /// </summary>
        /// <param name="d"></param>
        /// <param name="p"></param>
        private static void constructWallsOnPolygon(Dungeoneer d, Polygon p) {
            foreach (Edge e in p.sides) {
                constructWallOnEdge(d, e);
            }
        }

        /// <summary>
        /// Make walls along an arbitrary edge
        /// </summary>
        /// <param name="wall"></param>
        private static void constructWallOnEdge(Dungeoneer d, Edge wall) {
            // Since the prefab is stretched equally along x and y, it must be placed at the center for both x and y
            Vector3 p = wall.p.toVector3AtHeight(d.currentDungeonType.wallHeight/2);
            Vector3 q = wall.q.toVector3AtHeight(d.currentDungeonType.wallHeight/2);
            Vector3 wallCenter = (p+q)/2;

            GameObject wallObject = UnityEngine.Object.Instantiate(d.currentDungeonType.wall.prefab, wallCenter, d.currentDungeonType.wall.prefab.transform.rotation);
            wallObject.transform.parent = d.wallOrganizer.transform;
            wall.wallObjects.Add(wallObject);

            // Stretch wall
            wallObject.transform.localScale = new Vector3(
                wall.length,  // length of the original edge
                wallObject.transform.localScale.y * d.currentDungeonType.wallHeight,  // desired wall height
                wallObject.transform.localScale.z  // original prefab thickness
            );

            // Rotate the wall so that it's placed along the original edge
            Vector3 lookatme = Vector3.Cross(q - wallCenter, Vector3.up).normalized;
            wallObject.transform.LookAt(wallCenter + lookatme);

            // Attach info to game object for later use
            wallObject.GetComponent<WallInfo>().associatedEdge = wall;
            wall.isWall = true;

            /*
            Vector3 direction = (q-p).normalized;
            #if UNITY_EDITOR
            Debug.DrawRay(p, direction * wall.length, Color.white, 512);
            #endif
            */
        }

        /// <summary>
        /// Destroys wall objects where corridors should exist
        /// </summary>
        /// <param name="d"></param>
        /// <returns>list of walls that are valid</returns>
        private static List<GameObject> destroyWallsForCorridors(Dungeoneer d) {
            LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
            List<GameObject> wallsToKeep = new List<GameObject>();
            // Need to make sure we can fit through these ones
            List<Edge> shortDestroyedWalls = new List<Edge>();

            foreach (Edge e in d.spanningTree) {
                float len = e.length;

                Vector3 p = e.p.toVector3AtHeight(1);
                Vector3 q = e.q.toVector3AtHeight(1);
                Vector3 direction = (q-p).normalized;
                // uncomment to debug raycasts
                /*
                #if UNITY_EDITOR
                Debug.DrawRay(p, direction * len, Color.blue, 512);
                #endif
                */

                // Cast a ray to destroy initial set of walls. SphereCast does
                // not always catch all walls close to the sphere center
                List<RaycastHit> hits = new List<RaycastHit>(Physics.CapsuleCastAll(p, p + Vector3.up * d.currentDungeonType.wallHeight, d.currentDungeonType.hallWidth, direction, len, wallMask));
                foreach (RaycastHit hit in hits) {
                    Edge er = hit.collider.gameObject.GetComponent<WallInfo>().associatedEdge;
                    er.isWall = false;

                    if (er.length < FATTEST_CONTROLLER * 2) {
                        shortDestroyedWalls.Add(er);
                    }
                    hit.collider.gameObject.name = $"Destroyed {hit.collider.gameObject.name}";
                    UnityEngine.Object.Destroy(hit.collider.gameObject);
                }

                // Cast a wider sphere to determine what walls to keep during cleanup
                foreach (RaycastHit hit in Physics.SphereCastAll(p, d.currentDungeonType.hallWidth * 4, direction, len, wallMask)) {
                    wallsToKeep.Add(hit.collider.gameObject);
                }
            }

            // Sometimes, only a really short wall is removed, so the player still can't fit. Probably a Unity spherecast bug
            foreach (Edge w in shortDestroyedWalls) {
                int pwalls = w.p.incidentEdges.Count(e => e.isWall);
                int qwalls = w.q.incidentEdges.Count(e => e.isWall);
                float minp = (pwalls > 0)? w.p.incidentEdges.Where(e => e.isWall).Min(e => e.length) : 0;
                float minq = (qwalls > 0)? w.q.incidentEdges.Where(e => e.isWall).Min(e => e.length) : 0;
                Vertex v;
                // knock out the least-connected side
                if (pwalls < qwalls) {
                    v = w.p;
                } else if (pwalls == qwalls) {
                    if (pwalls == 0) {  // don't do anything if neither have a wall to remove...
                        continue;
                    }
                    v = (minp > minq)? w.q : w.p;
                } else {
                    v = w.q;
                }

                // remove the shortest edge
                // float equality is dumb, so this is ugly
                Edge shortestEdge = null;
                float s = Mathf.Infinity;
                foreach (Edge hurr in v.incidentEdges) {
                    if (hurr.isWall && hurr.length < s) {
                        shortestEdge = hurr;
                        s = hurr.length;
                    }
                }
                if (shortestEdge == null) {  // why does this happen?!
                    continue;
                }
                shortestEdge.isWall = false;
                foreach (GameObject wo in shortestEdge.wallObjects) {
                    wallsToKeep.Remove(wo);
                    UnityEngine.Object.Destroy(wo);
                }
            }

            return wallsToKeep;
        }

        /// <summary>
        /// Remove walls that don't need to exist for performance reasons
        /// </summary>
        /// <param name="d">dungeoneer object</param>
        /// <param name="wallsToKeep">list of walls to avoid destroying</param>
        private static void destroyLagWalls(Dungeoneer d, List<GameObject> wallsToKeep) {
            foreach (VoronoiRoom r in d.roomVoronoi) {
                foreach (Edge e in r.exterior.sides) {
                    wallsToKeep.AddRange(e.wallObjects);
                }
            }

            foreach (GameObject w in GameObject.FindGameObjectsWithTag("Wall")) {
                if (w != null && !wallsToKeep.Contains(w)) {
                    WallInfo wi = w.GetComponent<WallInfo>();
                    if (wi != null) {
                        wi.associatedEdge.isWall = false;
                    }
                    w.name = $"Destroyed {w.name}";
                    UnityEngine.Object.Destroy(w);
                }
            }
        }
    }
}
