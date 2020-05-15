using System.Linq;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace FiscalShock.Graphs {
    /// <summary>
    /// Vertex (or "node") as part of a graph, defined by
    /// 2D Cartesian coordinates
    /// </summary>
    public class Vertex {
        public Vector2 vector { get; }
        public float x => vector.x;
        public float y => vector.y;
        public int id { get; }
        public List<Triangle> triangles { get; } = new List<Triangle>();
        public Cell cell;

        /* Spending the space to track connected components simplifies
         * any algorithms that need to traverse a graph.
         */
        // Vertices adjacent (connected by an edge)
        public List<Vertex> neighborhood { get; set; } = new List<Vertex>();
        // Edges incident (having this vertex as an endpoint)
        public List<Edge> incidentEdges { get; set; } = new List<Edge>();

        // PATHFINDING ONLY
        /// <summary>
        /// Whether or not this vertex is usable in pathfinding.
        /// </summary>
        internal bool toIgnore { get; set; } = false;

        /* Begin overloaded constructors */
        /// <summary>
        /// Create a vertex from float coordinates
        /// </summary>
        public Vertex(float xX, float yY) {
            vector = new Vector2(xX, yY);
        }

        /// <summary>
        /// Create a vertex from float coordinates and assign an ID
        /// </summary>
        public Vertex(float xX, float yY, int vid) : this(xX, yY) {
            id = vid;
        }

        /// <summary>
        /// Create vertex from double coordinates and cast the doubles to
        /// floats
        /// </summary>
        public Vertex(double xX, double yY) : this((float)xX, (float)yY) {}

        /// <summary>
        /// Create vertex from an array of doubles
        /// </summary>
        /// <param name="xy">2-element array in the form of [x, y]</param>
        /// <returns></returns>
        public Vertex(double[] xy) : this(xy[0], xy[1]) {
            if (xy.Length > 2) {
                Debug.LogError($"FATAL: Input array held more than two coordinates.");
                throw new ArgumentException();
            }
        }
        /* End overloaded constructors */

        /* Comparator functions - needed for LINQ */
        /// <summary>
        /// Check equality based on the backing Vector2
        /// </summary>
        /// <param name="obj">other object to compare to</param>
        /// <returns>equality</returns>
        public override bool Equals(object obj) {
            if (obj is Vertex other) {
                return vector == other.vector;
            }
            return false;
        }

        /// <summary>
        /// Taken from https://stackoverflow.com/a/2280213
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            int hash = 23;
            hash = (hash * 31) + x.GetHashCode();
            hash = (hash * 31) + y.GetHashCode();
            return hash;
        }
        /* End comparator functions */

        /// <summary>
        /// Euclidean distance between two Cartesian coordiates
        /// </summary>
        /// <param name="other">distant vertex</param>
        /// <returns>distance</returns>
        public double getDistanceTo(Vertex other) {
            return Mathy.getDistanceBetween(x, y, other.x, other.y);
        }

        /// <summary>
        /// Given a list of vertices and an origin vertex, find the nearest one
        /// (via Euclidean distance).
        /// </summary>
        /// <param name="others"></param>
        /// <returns></returns>
        public Vertex findNearestInList(List<Vertex> others) {
            // Find all distances to the origin
            List<double> distances = others.Select(v => v.getDistanceTo(this)).ToList();

            /* If origin is in the list, we want the second minimum distance.
             * This means we can't just take the minimum of this list.
             * But we can just choose to skip the first element in the sorted list instead, or skip na
             */
            int skip = others.Contains(this)? 1 : 0;
            double minimumDistance = distances.OrderBy(d => d).Skip(skip).First();
            int indexOfNearest = distances.IndexOf(minimumDistance);

            return others[indexOfNearest];
        }

        /// <summary>
        /// Get the angle of rotation from this vertex to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public float getAngleOfRotationTo(Vertex other) {
            return (float)Mathy.getAngleOfRotation(x, y, other.x, other.y);
        }

        /// <summary>
        /// Convert to Unity Vector3
        /// Unity uses y-axis as height (up/down) and z-axis as depth, unlike
        /// e.g. Blender where z is up/down
        /// Since a Vertex is 2D, the third dimension must be specified
        /// </summary>
        /// <param name="height">desired height of Vector3</param>
        /// <returns>3D vertex</returns>
        public Vector3 toVector3AtHeight(float height) {
            return new Vector3(x, height, y);
        }
    }
}
