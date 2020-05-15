using System.Collections.Generic;

namespace FiscalShock.Graphs {
    /// <summary>
    /// Undirected edge, defined by endpoints
    /// For only Delaunator edges, ID is relevant
    /// </summary>
    public class Edge {
        public List<Vertex> vertices { get; }
        public Vertex p => vertices[0];
        public Vertex q => vertices[1];
        public List<Cell> cells { get; } = new List<Cell>();
        public List<UnityEngine.GameObject> wallObjects = new List<UnityEngine.GameObject>();
        public float length { get; }
        public bool isWall { get; set; }

        // PATHFINDING ONLY
        /// <summary>
        /// Whether or not this edge is usable in pathfinding.
        /// </summary>
        internal bool toIgnore { get; set; } = false;

        /// <summary>
        /// Create an edge between two vertices. Must call
        /// `connect` on the resulting edge with the two vertices
        /// to update neighborhoods and incident edges!
        /// </summary>
        /// <param name="a">vertex a</param>
        /// <param name="b">vertex b</param>
        public Edge(Vertex a, Vertex b) {
            vertices = new List<Vertex> { a, b };
            length = getLength();
        }

        /// <summary>
        /// Create an edge based on two Unity Vector3s
        /// </summary>
        /// <param name="a">vertex a</param>
        /// <param name="b">vertex b</param>
        public Edge(UnityEngine.Vector3 a, UnityEngine.Vector3 b) {
            Vertex va = new Vertex(a.x, a.z);
            Vertex vb = new Vertex(b.x, b.z);
            vertices = new List<Vertex> { va, vb };
            length = getLength();
        }

        /// <summary>
        /// Properly update the neighborhoods and incident edges of
        /// the given vertices. This is separate from the Edge constructor
        /// so that it's possible to create edges for temporary use without
        /// mucking up the actual vertices involved, since these are all
        /// call by reference.
        /// Maybe this should just connect the two vertices p and q of
        /// this edge...
        /// </summary>
        /// <param name="a">vertex a</param>
        /// <param name="b">vertex b</param>
        public void connect(Vertex a, Vertex b) {
            a.neighborhood.Add(b);
            b.neighborhood.Add(a);
            a.incidentEdges.Add(this);
            b.incidentEdges.Add(this);
        }

        /* Delaunator-only helper functions */
        /// <summary>
        /// Delaunator helper function to determine the index of the triangle
        /// that an edge belongs to
        /// </summary>
        /// <param name="eid">id of an edge</param>
        /// <returns></returns>
        public static int getTriangleId(int eid) {
            return eid / 3;
        }
        /* End Delaunator helper functions */

        /* Comparator functions - needed for LINQ */
        /// <summary>
        /// Equal check. Two edges are equal if their endpoints
        /// are the same (in any order).
        /// </summary>
        /// <param name="obj">object to compare to</param>
        /// <returns>equality</returns>
        public override bool Equals(object obj) {
            if (obj is Edge other) {
                return (p.Equals(other.p) && q.Equals(other.q)) || (p.Equals(other.q) && q.Equals(other.p));
            }
            return false;
        }

        /// <summary>
        /// Taken from https://stackoverflow.com/a/2280213.
        /// Needed for LINQ comparisons.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            int hash = 23;
            hash = (hash * 31) + p.GetHashCode();
            hash = (hash * 31) + q.GetHashCode();
            return hash;
        }
        /* End comparator functions */

        /// <summary>
        /// Length of an edge in this case is the Euclidean distance
        /// between the endpoints
        /// </summary>
        /// <returns>length of this edge</returns>
        private float getLength() {
            return (float)p.getDistanceTo(q);
        }

        /// <summary>
        /// Angle of this edge from p to q
        /// </summary>
        /// <returns></returns>
        public float getAngle() {
            return p.getAngleOfRotationTo(q);
        }

        /// <summary>
        /// Find the intersection between two edges.
        /// </summary>
        /// <param name="other">other edge</param>
        /// <returns>intersection point</returns>
        public Vertex findIntersection(Edge other) {
            double[] vertices = Mathy.findIntersection(
                p.vector.x, p.vector.y,
                q.vector.x, q.vector.y,
                other.p.vector.x, other.p.vector.y,
                other.q.vector.x, other.q.vector.y
            );

            if (vertices != null) {
                return new Vertex(vertices[0], vertices[1]);
            }
            return null;
        }
    }
}
