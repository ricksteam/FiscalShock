using UnityEngine;
using System;
using FiscalShock.Graphs;

namespace FiscalShock.Pathfinding {
    /// <summary>
    /// Wrapper class around Vertex class used for pathfinding purposes.
    /// </summary>
    public class VertexNode : IEquatable<VertexNode> {
        /// <summary>
        /// The Vertex associated with this VertexNode.
        /// </summary>
        public Vertex associatedLocation { get; }

        /// <summary>
        /// The node that leads to this node on the path from the start point.
        /// </summary>
        public VertexNode previousOnPath { get; private set; }

        /// <summary>
        /// The actual distance from the start point to this node following the path.
        /// </summary>
        public double gCost { get; private set; }

        /// <summary>
        /// The estimated, straight-line distance from this node to the destination.
        /// </summary>
        public double hCost { get; private set; }

        /// <summary>
        /// The estinated distance from the start to the destination by traveling via this node.
        /// In other words, the sum of the gCost and the hCost.
        /// </summary>
        public double fCost { get; private set; }

        /// <summary>
        /// Constructor that creates VertexNode without initializing any costs.
        /// </summary>
        /// <param name="associatedVertex">The Vertex associated with this VertexNode.</param>
        public VertexNode(Vertex associatedVertex) {
            associatedLocation = associatedVertex;
            previousOnPath = null;
        }

        /// <summary>
        /// Constructor that creates VertexNode and initializes costs.
        /// </summary>
        /// <param name="associatedVertex">The Vertex associated with this VertexNode.</param>
        /// <param name="g">The cost from the start to this location.</param>
        /// <param name="h">The straight line cost from this location to the destination.</param>
        public VertexNode(Vertex associatedVertex, double g, double h) {
            associatedLocation = associatedVertex;
            gCost = g;
            hCost = h;
            fCost = g + h;
            previousOnPath = null;
        }

        /// <summary>
        /// Sets the VertexNode that leads to this one.
        /// </summary>
        /// <param name="parent">The VertexNode that leads to this one on the path.</param>
        public void setLinkToPrevious(VertexNode parent) {
            previousOnPath = parent;
        }

        /// <summary>
        /// Set the costs of this location.
        /// </summary>
        /// <param name="g">The cost from the start point to this location.</param>
        /// <param name="h">The straight-line distance from this point to the destination.</param>
        public void setCosts(double g, double h) {
            gCost = g;
            hCost = h;
            fCost = g+h;
        }

        /// <summary>
        /// Sets the gCost and recalculates the fCost.
        /// </summary>
        /// <param name="newG">The updated gCost for this node.</param>
        public void changeGCost(double newG) {
            gCost = newG;
            fCost = newG + hCost;
        }

        /// <summary>
        /// For necessary LINQ compliance. Determines if the locations of two VertexNodes
        /// are the same.
        /// </summary>
        /// <param name="other">The VertexNode whose equality to this one we are determining.</param>
        /// <returns>True if the values are equal, false if they're not.</returns>
        public bool Equals(VertexNode other) {
            return associatedLocation.Equals(other.associatedLocation);
        }

        /// <summary>
        /// Override for IEquatable method. Determines if the locations of this VertexNode and
        /// obj are the same.
        /// </summary>
        /// <param name="obj">Generic object that may or may not be an equivalent VertexNode.</param>
        /// <returns>True if the values are equal, false if they're not.</returns>
        public override bool Equals(object obj) {
            if (obj is VertexNode other) {
                return associatedLocation.Equals(other.associatedLocation);
            }

            return false;
        }

        // Source:
        // https://codereview.stackexchange.com/questions/164970/using-gethashcode-in-equals
        // https://stackoverflow.com/questions/720177/default-implementation-for-object-gethashcode/720282#720282
        // This should work -- only VertexNodes being stored in any data structure -- but proceed with caution.

        /// <summary>
        /// Override for LINQ compliance. Uses the associated Vertex <c>hashCode()</c> function and returns
        /// the same.
        /// </summary>
        /// <returns>An integer representing the hash code for this object.</returns>
        public override int GetHashCode() {
            return associatedLocation.GetHashCode();
        }
    }
}
