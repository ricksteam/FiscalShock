using UnityEngine;
using System;
using System.Collections.Generic;
using FiscalShock.Graphs;
using System.IO;

// Algorithm Source: https://theory.stanford.edu/~amitp/GameProgramming/ImplementationNotes.html
namespace FiscalShock.Pathfinding {
    /// <summary>
    /// The AStar class instance stores an instance of the navigation graph and
    /// allows the user to find a path to some destination using the graph.
    /// </summary>
    public class AStar {
        /*
            Steps for Debugging this Class:
            1) Check the script execution order if obtaining null reference exceptions.
            2) Check if one of the data structures below is running out of nodes to visit prematurely.
        */

        /// <summary>
        /// The Delaunay Triangulation used by AStar to find the path.
        /// </summary>
        private Delaunay navGraph;

        /// <summary>
        /// Instantiates AStar using the given Delaunay Triangulation.
        /// </summary>
        /// <param name="graph"></param>
        public AStar(Delaunay graph) {
            navGraph = graph;

            // DEBUG: Uncomment if pathfinding not working correctly.
            // Debug.Log("AStar is working. Graph: " + navGraph);
            // outputGraphToFile();
        }

        /// <summary>
        /// Method used to determine the shortest path to the destination
        /// point from the start point using the A* algorithm.
        /// </summary>
        /// <param name="start">The point at which to start the pathfinding.</param>
        /// <param name="destination">The target point of the pathfinding.</param>
        /// <returns>Stack containing the vertices that create the shortest path, with the top as the start point.</returns>
        public Stack<Vertex> findPath(Vertex start, Vertex destination) {
            Stack<Vertex> path = new Stack<Vertex>();

            // Return an empty path when the start and destination are the same.
            if (start.Equals(destination)) {
                return path;
            }

            // Create the set of open vertices.
            LinkedList<VertexNode> open = new LinkedList<VertexNode>();

            // Add starting node to the list, and set its parent to null.
            open.AddFirst(new VertexNode(start, 0, destination.getDistanceTo(start)));
            open.First.Value.setLinkToPrevious(null);

            // Create the set of closed vertices.
            LinkedList<VertexNode> closed = new LinkedList<VertexNode>();

            // Create and initialize the vertex that's currently being checked.
            LinkedListNode<VertexNode> currentNode = open.Last;

            // G-cost of a neighboring vertex.
            double cost;

            // Filled if a node already exists in the open or closed sets.
            // Should theoretically never be filled together.
            LinkedListNode<VertexNode> openTemp, closedTemp;

            // Make sure that the currently checked node is not the destination node.
            while (!currentNode.Value.associatedLocation.Equals(destination)) {
                // Remove the node with the smallest f-cost from open and add it to closed.
                open.RemoveLast();
                closed.AddFirst(currentNode);

                // A node should be placed in open when:
                // A) It hasn't been visited yet. (Neither in open nor closed.)
                // B) It's already in open, but the gCost is smaller than that of the existing node in the set.
                // C) It's in the closed set, but the gCost is better than the original visit.
                foreach (Vertex neighbor in currentNode.Value.associatedLocation.neighborhood) {
                    // Neighboring vertices that are unnavigable are useless.
                    if (neighbor.toIgnore) {
                        continue;
                    }

                    // Calculate the g-cost of this neighbor and prepare it for use in the lists.
                    cost = currentNode.Value.gCost + currentNode.Value.associatedLocation.getDistanceTo(neighbor);
                    VertexNode neighborNode = new VertexNode(neighbor);

                    // Obtain the equivalent node in open, or null if it's not in the set.
                    openTemp = open.Find(neighborNode);

                    // Check if the node's position shoud change in the list. 
                    if (openTemp != null && openTemp.Value.gCost > cost) {
                        open.Remove(openTemp);
                    }

                    // Check if the node exists in closed and should be placed in the open set again.
                    closedTemp = closed.Find(neighborNode);
                    if (closedTemp != null && closedTemp.Value.gCost > cost) {
                        closed.Remove(closedTemp);
                    }

                    // Add the node to the open set if doesn't exist in there.
                    if (!open.Contains(neighborNode) && !closed.Contains(neighborNode)) {
                        // If value already existed in one of sets, replace node with the node
                        // from the set and update the gCost.
                        if (openTemp != null) {
                            neighborNode = openTemp.Value;
                            neighborNode.changeGCost(cost);
                        }

                        else if (closedTemp != null) {
                            neighborNode = closedTemp.Value;
                            neighborNode.changeGCost(cost);
                        }

                        // The node didn't already exist, so need to instantiate the node with new values.
                        else {
                            neighborNode.setCosts(cost, destination.getDistanceTo(neighborNode.associatedLocation));
                        }

                        // Set this node to the previous node, then add the neighbor to the open set.
                        neighborNode.setLinkToPrevious(currentNode.Value);
                        sortedAdd(open, neighborNode);
                    }
                }

                // Set current to the node with the smallest fcost/gcost.
                currentNode = open.Last;
            }

            // A path has been found. Take the associated VertexNode from the LinkedListNode.
            VertexNode node = currentNode.Value;

            // Use the parents to rebuild the path back to the start point.
            while (node != null) {
                path.Push(node.associatedLocation);
                node = node.previousOnPath;
            }

            // DEBUG: Uncomment if code not working as expected.
            // Debug.Log("TOTAL NODES PASSED: " + path.Count);

            return path;
        }

        /// <summary>
        /// Method that takes a node and inserts it into the given list in greatest
        /// to least order. Insertion inserts in order of greatest f-cost first,
        /// then greatest h-cost as a tie breaker.
        /// </summary>
        /// <param name="list">List into which to insert the node.</param>
        /// <param name="node">The node to insert into the list.</param>
        private void sortedAdd(LinkedList<VertexNode> list, VertexNode node) {
            // Obtain the first node, since have to traverse the list from the start.
            LinkedListNode<VertexNode> currentNode = list.First;

            // Can just insert if the list is empty.
            if (list.Count == 0) {
                list.AddFirst(node);
                return;
            }

            // Insert into a non-empty list.
            while(true) {
                // When the fcosts are equal, have to check hcost to determine insertion position.
                if(currentNode.Value.fCost == node.fCost) {
                    if(node.hCost >= currentNode.Value.hCost) {
                        list.AddBefore(currentNode, node);
                        break;
                    }
                }

                if(node.fCost > currentNode.Value.fCost) {
                    list.AddBefore(currentNode, node);
                    break;
                }

                if(currentNode.Next == null) {
                    list.AddAfter(currentNode, node);
                    break;
                }

                // If it didn't break by this point, move onto the next list element.
                currentNode = currentNode.Next;
            }
        }

        /// <summary>
        /// Method used to write the vertices and their neighboring vertices.
        /// Used mainly to debug graph finding code.
        /// </summary>
        private void outputGraphToFile() {
            StreamWriter writer = new StreamWriter(string.Format("{0}/astar_vertices.txt", Directory.GetCurrentDirectory()));

            foreach (Vertex vertex in navGraph.vertices) {
                writer.Write("VERTEX: " + vertex.vector + "\n");

                foreach (Vertex v in vertex.neighborhood) {
                    if (!v.toIgnore) {
                        writer.Write("\tNEIGHBOR VERTEX: " + v.vector + "\n");
                    }
                }
            }

            writer.Close();
        }
    }
}
