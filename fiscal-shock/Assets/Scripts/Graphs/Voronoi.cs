using System.Linq;
using System.Collections.Generic;

namespace FiscalShock.Graphs {
    /// <summary>
    /// Data structure representing a Voronoi diagram.
    /// </summary>
    public class Voronoi : Graph {
        /// <summary>
        /// Corresponds to the vertices of the Delaunay triangulation
        /// used to create this graph.
        /// Anywhere within a given cell, there exists one site, which is
        /// the closest site, no matter where one stands within that cell's
        /// boundaries.
        ///
        /// <para>WARNING: Reference to the dual's vertices -- don't mutate!</para>
        /// </summary>
        public List<Vertex> sites { get; }

        /// <summary>
        /// Polygons representing faces of the Voronoi diagram.
        /// </summary>
        public List<Cell> cells { get; }
        public Delaunay dual { get; }

        /// <summary>
        /// Generates a Voronoi diagram using Delaunator output
        /// </summary>
        /// <param name="del"></param>
        public Voronoi(Delaunay del) {
            dual = del;
            sites = dual.vertices;
            cells = new List<Cell>();

            createCells();
            calculateVerticesAndEdgesFromDelaunator();
            calculateCellAreas();
        }

        /// <summary>
        /// Algorithm taken from Delaunator guide.
        /// <para>https://mapbox.github.io/delaunator/</para>
        /// </summary>
        private void calculateVerticesAndEdgesFromDelaunator() {
            for (int e = 0; e < dual.delaunator.triangles.Count; e++) {
                if (e < dual.delaunator.halfedges[e]) {
                    Triangle triP = dual.triangles[Edge.getTriangleId(e)];
                    Triangle triQ = dual.triangles[Edge.getTriangleId(dual.delaunator.halfedges[e])];
                    Vertex p = triP.findCircumcenter();
                    Vertex q = triQ.findCircumcenter();

                    Edge pq = new Edge(p, q);

                    vertices.Add(p);
                    vertices.Add(q);
                    edges.Add(pq);
                    pq.connect(p, q);

                    List<Vertex> triPVertices = triP.vertices;
                    List<Vertex> triQVertices = triQ.vertices;
                    foreach (Vertex vertex in triPVertices) {
                        if (triQVertices.Contains(vertex)) {
                            vertex.cell.sides.Add(pq);
                            pq.cells.Add(vertex.cell);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cell neighbors are just adjacent sites in the Delaunay triangulation.
        /// </summary>
        private void createCells() {
            // First, construct the list completely
            foreach (Vertex site in sites) {
                Cell cell = new Cell(site);
                cells.Add(cell);
            }
            // Now we can reference other cells farther down the list
            foreach (Cell cell in cells) {
                foreach (Vertex neighbor in cell.site.neighborhood) {
                    cell.neighbors.Add(cells[neighbor.id]);
                }
            }
        }

        /// <summary>
        /// Calculate and store all cell areas. Don't run this until cells have
        /// been found properly.
        /// </summary>
        private void calculateCellAreas() {
            foreach (Cell c in cells) {
                if (dual.convexHull.Contains(c.site)) {
                    c.area = 0;
                } else {
                    c.setVerticesFromSides();
                    c.orderVertices();
                    c.area = c.getArea();
                }
            }
        }
    }

    /// <summary>
    /// Data structure representing a collection of Voronoi cells that comprise
    /// a dungeon "room."
    /// </summary>
    public class VoronoiRoom : Graph {
        public List<Cell> cells { get; private set; } = new List<Cell>();
        public Vertex site { get; }
        public List<Edge> allEdges => cells.SelectMany(c => c.sides).Distinct().ToList();
        public Polygon exterior { get; private set; }
        public List<Edge> interiorEdges { get; private set; }
        public Polygon boundingBox { get; private set; }
        //new public List<Vertex> vertices => cells.SelectMany(c => c.vertices).Distinct().ToList();

        /// <summary>
        /// sentinel value to prevent adding rude cells near convex hull
        /// </summary>
        public static readonly double MAX_CELL_AREA = 2048;

        /// <summary>
        /// Create a new room, seeded by a single Delaunay vertex
        /// </summary>
        /// <param name="newSite"></param>
        public VoronoiRoom(Vertex newSite) {
            if (newSite.cell.getArea() < MAX_CELL_AREA) {
                site = newSite;
                cells.Add(site.cell);
                site.cell.room = this;
                setExterior();
                setVertices();
                setEdges();
            } else {
                UnityEngine.Debug.Log($"This site's cell was too large: {newSite.cell.getArea()}");
            }
        }

        /// <summary>
        /// Any edge not included in another cell is part of the exterior.
        /// </summary>
        private void setExterior() {
            List<Edge> ext = cells
                .SelectMany(c => c.sides)
                .GroupBy(e => e)
                .Where(g => g.Count() == 1)
                .Select(g => g.First())
                .ToList();

            exterior = new Polygon(ext);
            setBoundingBox();
            setInteriorEdges();
        }

        /// <summary>
        /// Recalculate interior edges, which are all edges not included in
        /// the list of exterior edges. Should only be called by `setExterior`.
        /// </summary>
        private void setInteriorEdges() {
            interiorEdges = cells
                .SelectMany(c => c.sides)
                .Except(exterior.sides)
                .ToList();
        }

        /// <summary>
        /// Expands the room by adding all neighbors of current cells
        /// </summary>
        public void grow() {
            int originalCellCount = cells.Count;
            for (int i = 0; i < originalCellCount; ++i) {
                foreach (Cell c in cells[i].neighbors) {
                    if (c.isClosed && c.getArea() < MAX_CELL_AREA && c.room == null) {
                        cells.Add(c);
                        c.room = this;
                    }
                }
            }
            // clear out duplicates
            cells = cells.Distinct().ToList();
            setExterior();
            setVertices();
            setEdges();
        }

        private void setBoundingBox() {
            boundingBox = exterior.getBoundingBox();
        }

        private void setVertices() {
            vertices = cells.SelectMany(c => c.vertices).Distinct().ToList();
        }

        private void setEdges() {
            edges = cells.SelectMany(c => c.sides).Distinct().ToList();
        }
    }
}
