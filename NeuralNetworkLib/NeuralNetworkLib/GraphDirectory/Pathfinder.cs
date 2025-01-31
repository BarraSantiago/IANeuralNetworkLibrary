using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.GraphDirectory
{
    public abstract class Pathfinder<TNodeType, TCoordinateType, TCoordinate>
        where TNodeType : INode<TCoordinateType>
        where TCoordinateType : IEquatable<TCoordinateType>, IVector
        where TCoordinate : ICoordinate<TCoordinateType>, new()
    {
        protected TNodeType[,] Graph;

        /// <summary>
        /// Finds a path from startNode to destinationNode using a multi-threaded A*-like approach
        /// with a priority queue (min-heap). 
        /// </summary>
        public List<TNodeType> FindPath(TNodeType startNode, TNodeType destinationNode)
        {
            // Because we may have up to 40k nodes, let's store node data in dictionaries
            // or arrays (depending on your node indexing). Here we use dictionaries keyed by TNodeType.
            // For large grids, consider storing these in arrays if you can map TNodeType -> x,y indices.
            var gCost = new ConcurrentDictionary<TNodeType, int>();
            var cameFrom = new ConcurrentDictionary<TNodeType, TNodeType>();

            // Heuristic is typically estimated from the current node to the GOAL, 
            // not from the node to the START. We'll define a small function for that:
            int HeuristicCost(TNodeType from, TNodeType to)
            {
                TCoordinate fromCoord = new TCoordinate();
                fromCoord.SetCoordinate(from.GetCoordinate());

                TCoordinate toCoord = new TCoordinate();
                toCoord.SetCoordinate(to.GetCoordinate());

                return Distance(fromCoord, toCoord);
            }

            // We'll initialize costs and parent for every node to "infinity" or an extremely large number
            // except the start node
            foreach (var node in Graph)
            {
                gCost[node] = int.MaxValue;
            }
            gCost[startNode] = startNode.GetCost(); // or typically 0 if you want to ignore the start cost

            // A min-heap priority queue for (node, priority = fCost = gCost + heuristic).
            // .NET 7's PriorityQueue usage:
            var openQueue = new PriorityQueue<TNodeType, int>();

            // Enqueue the start node with its initial priority
            openQueue.Enqueue(startNode, gCost[startNode] + HeuristicCost(startNode, destinationNode));

            // A thread-safe set to mark visited / closed nodes (or a dictionary).
            // Because we pop from the queue in ascending order of fCost, once a node is closed, 
            // we don't revisit it.
            var closedSet = new ConcurrentDictionary<TNodeType, bool>();

            while (openQueue.Count > 0)
            {
                // Dequeue the node with the smallest (gCost + heuristic)
                var current = openQueue.Dequeue();

                // If we've reached the goal, reconstruct the path
                if (NodesEquals(current, destinationNode))
                {
                    return ReconstructPath(cameFrom, startNode, destinationNode);
                }

                // If we've already closed this node, skip
                if (!closedSet.TryAdd(current, true))
                {
                    // It was already in closedSet
                    continue;
                }

                // Expand neighbors in parallel
                var neighbors = GetNeighbors(current);

                Parallel.ForEach(neighbors, neighbor =>
                {
                    var neighborNode = Graph[(int)neighbor.X, (int)neighbor.Y];

                    // If neighbor is blocked or already closed, skip
                    if (IsBlocked(neighborNode) || closedSet.ContainsKey(neighborNode)) 
                        return;

                    int tentative_g = gCost[current] 
                                      + MoveToNeighborCost(current, neighborNode);

                    // If the new route to neighbor is better, update and push
                    if (tentative_g < gCost[neighborNode])
                    {
                        gCost[neighborNode] = tentative_g;
                        cameFrom[neighborNode] = current;

                        int fCost = tentative_g + HeuristicCost(neighborNode, destinationNode);

                        // IMPORTANT: PriorityQueue is not thread-safe. We must lock around it:
                        lock (openQueue)
                        {
                            openQueue.Enqueue(neighborNode, fCost);
                        }
                    }
                });
            }

            // If we exhausted the queue without finding a path
            return null;
        }

        /// <summary>
        /// Reconstructs the path from cameFrom data.
        /// </summary>
        private List<TNodeType> ReconstructPath(
            ConcurrentDictionary<TNodeType, TNodeType> cameFrom,
            TNodeType start,
            TNodeType goal)
        {
            var path = new List<TNodeType>();
            var current = goal;

            while (!NodesEquals(current, start))
            {
                path.Add(current);
                // If we can't find the parent (parallel race?), break or handle
                if (!cameFrom.TryGetValue(current, out var parent))
                {
                    break;
                }
                current = parent;
            }
            path.Add(start); // Add the start node
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Given two node coordinates, compute an integer distance.
        /// Typically for A*, we might do Manhattan or Euclidean.
        /// </summary>
        protected abstract int Distance(TCoordinate a, TCoordinate b);

        /// <summary>
        /// Return the valid neighbors for a given node (coordinate offsets, adjacency, etc.)
        /// </summary>
        protected abstract ICollection<TCoordinateType> GetNeighbors(TNodeType node);

        /// <summary>
        /// Return whether two TNodeType references are "the same" node
        /// </summary>
        protected abstract bool NodesEquals(TNodeType A, TNodeType B);

        /// <summary>
        /// Return the cost of moving from node A to node B (often = B.GetCost() or 1).
        /// </summary>
        protected abstract int MoveToNeighborCost(TNodeType A, TNodeType B);

        /// <summary>
        /// Return true if the node is blocked (wall, obstacle).
        /// </summary>
        protected abstract bool IsBlocked(TNodeType node);
    }
}
