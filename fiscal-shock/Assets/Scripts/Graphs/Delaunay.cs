using System.Collections.Generic;
using ThirdParty.Delaunator;
using System.Linq;
using UnityEngine;

namespace FiscalShock.Graphs {
    /// <summary>
    /// Data structure representing a Delaunay triangulation.
    /// Interface and extension of the Delaunator library. Turns the Delaunator
    /// results into less-performant but easier to use data structures.
    /// </summary>
    public class Delaunay : Graph {
        /// <summary>
        /// Reference to the raw Delaunator data.
        /// </summary>
        public Triangulation delaunator { get; private set; }

        /// <summary>
        /// List of triangles in this triangulation.
        /// </summary>
        public List<Triangle> triangles { get;} = new List<Triangle>();

        /// <summary>
        /// This graph's dual, a Voronoi diagram.
        /// </summary>
        public Voronoi dual { get; set; }

        /// <summary>
        /// The convex hull of this graph.
        /// <para>WARNING: Convex hull function implementation is likely incorrect.</para>
        /// </summary>
        public List<Vertex> convexHull { get; private set; } = new List<Vertex>();
        public List<Edge> convexHullEdges { get; private set; } = new List<Edge>();

        /// <summary>
        /// Construct a triangulation from a flattened list of doubles.
        /// </summary>
        /// <param name="input">List of n doubles in the format (x1, y1, x2, y2, ..., xn, yn)</param>
        public Delaunay(List<double> input) {
            fromDoubleList(input);
        }

        /// <summary>
        /// Generate Delaunay using a list of vertices
        /// </summary>
        /// <param name="input">vertices</param>
        public Delaunay(List<Vertex> input) {
            List<double> flat = new List<double>();
            foreach (Vertex v in input) {
                flat.Add(v.x);
                flat.Add(v.y);
            }
            fromDoubleList(flat);
            foreach (Vertex v in vertices) {
                // set cells to be the same, the reference is lost
                v.cell = input
                            .Where(x => x.vector == v.vector)
                            .Select(x => x.cell)
                            .FirstOrDefault();
            }
        }

        /// <summary>
        /// Generate filtered Delaunay by using cell list to cut off unneeded
        /// edges.
        /// </summary>
        /// <param name="del">Input triangulation used as base.</param>
        /// <param name="reachableCells">List of reachable cells used to filter graph.</param>
        public Delaunay(Delaunay del, List<Cell> reachableCells) {
            // Filter out vertices (cells) that aren't reachable. Mark them as ignored for pathfinding.
            vertices = del.vertices.Where(v => {
                v.toIgnore = !reachableCells.Contains(v.cell);
                return !v.toIgnore;
            }).ToList();

            // Filter out edges that lead to unreachable cells. Mark them as ignored for pathfinding.
            edges = del.edges.Where(e => {
                e.toIgnore = !vertices.Contains(e.p) || !vertices.Contains(e.q);
                return !e.toIgnore;
            }).ToList();
        }

        /// <summary>
        /// Inner method to do Delaunay from vertices
        /// </summary>
        /// <param name="input">List of n doubles in the format (x1, y1, x2, y2, ..., xn, yn)</param>
        private void fromDoubleList(List<double> input) {
            delaunator = new Triangulation(input);

            // Set up data structures for use in other scripts
            setTypedGeometry();
            convexHull = findConvexHull();
            List<Edge> hull = new List<Edge>();
            for (int i = 0; i < convexHull.Count; ++i) {
                if (i + 1 == convexHull.Count) {  // wrap around
                    hull.Add(new Edge(convexHull[i], convexHull[0]));
                } else {
                    hull.Add(new Edge(convexHull[i], convexHull[i+1]));
                }
            }
            convexHullEdges = hull;
        }

        /// <summary>
        /// Sets up all geometry from the triangulation into data structures
        /// that are easier to deal with
        /// </summary>
        public void setTypedGeometry() {
            // Get all vertices from the Delaunator triangulation
            List<double> triCoords = delaunator.coords;
            for (int i = 0; i < triCoords.Count; i += 2) {
                vertices.Add(new Vertex((float)triCoords[i], (float)triCoords[i + 1], vertices.Count));
            }

            // Simultaneously make edges and triangles without duplication
            List<List<int>> delEdges = new List<List<int>>();
            List<int> delTriangles = delaunator.triangles;
            List<List<List<int>>> triangleEdges = new List<List<List<int>>>();
            int triangleNum = 0;
            for (int i = 0; i < delTriangles.Count; i += 3) {
                int a = delTriangles[i];
                int b = delTriangles[i + 1];
                int c = delTriangles[i + 2];

                while (delEdges.Count <= a || delEdges.Count <= b || delEdges.Count <= c) {
                    delEdges.Add(new List<int>());
                    triangleEdges.Add(new List<List<int>>());
                }

                addToEdgeList(a, b, triangleNum, delEdges, triangleEdges);
                addToEdgeList(b, c, triangleNum, delEdges, triangleEdges);
                addToEdgeList(a, c, triangleNum, delEdges, triangleEdges);

                List<Vertex> vabc = new List<Vertex> { vertices[a], vertices[b], vertices[c] };

                Triangle t = new Triangle(
                    vabc,
                    i
                );
                triangles.Add(t);
                triangleNum++;
            }

            // Associate edges, triangles, and vertices
            for (int i = 0; i < delEdges.Count; i++) {
                for (int j = 0; j < delEdges[i].Count; j++) {
                    Edge edge = new Edge(vertices[i], vertices[delEdges[i][j]]);
                    edge.connect(vertices[i], vertices[delEdges[i][j]]);
                    edges.Add(edge);
                    foreach (int t in triangleEdges[i][delEdges[i][j]]){
                        triangles[t].sides.Add(edge);
                        vertices[i].triangles.Add(triangles[t]);
                        vertices[delEdges[i][j]].triangles.Add(triangles[t]);
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to create Delaunay data structures.
        /// Don't think too hard about this one and check their guide...
        /// <para>https://mapbox.github.io/delaunator/</para>
        /// </summary>
        /// <param name="a">index of edge a</param>
        /// <param name="b">index of edge b</param>
        /// <param name="triangleNum">which triangle this is</param>
        /// <param name="edges">list of edge indices</param>
        /// <param name="triangleEdges">list of triangle's edge indices</param>
        public static void addToEdgeList(int a, int b, int triangleNum, List<List<int>> edges, List<List<List<int>>> triangleEdges) {
            if (a < b) {
                while (triangleEdges[a].Count <= b) {
                    triangleEdges[a].Add(new List<int>());
                }
                triangleEdges[a][b].Add(triangleNum);

                if(!edges[a].Contains(b)) {
                    edges[a].Add(b);
                }
            } else {
                while (triangleEdges[b].Count <= a) {
                    triangleEdges[b].Add(new List<int>());
                }
                triangleEdges[b][a].Add(triangleNum);
                if (!edges[b].Contains(a) ){
                    edges[b].Add(a);
                }
            }
        }

        /// <summary>
        /// Generates the corresponding Voronoi diagram for this triangulation
        /// </summary>
        /// <returns>dual graph</returns>
        public Voronoi makeVoronoi() {
            dual = new Voronoi(this);
            return dual;
        }
    }
}
