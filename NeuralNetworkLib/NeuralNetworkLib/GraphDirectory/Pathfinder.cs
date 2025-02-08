using System.Collections.Concurrent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.GraphDirectory;

public abstract class Pathfinder<TNodeType, TCoordinateType, TCoordinate>
    where TNodeType : INode<TCoordinateType>
    where TCoordinateType : IEquatable<TCoordinateType>, IVector
    where TCoordinate : ICoordinate<TCoordinateType>, new()
{
    protected TNodeType[,] Graph;

    /// <summary>
    /// Finds a path from startNode to destinationNode using a multi-threaded A*-like approach
    /// with a priority queue (min-heap). If the destination node is blocked, it will attempt to
    /// find an alternative adjacent node (using GetAlternativeCoordinates) and route to that node.
    /// </summary>
    public List<TNodeType> FindPath(TNodeType startNode, TNodeType destinationNode)
    {
        if (IsBlocked(destinationNode))
        {
            ICollection<TCoordinateType> alternatives = GetAlternativeCoordinates(destinationNode.GetCoordinate());
            bool alternativeFound = false;
            foreach (var altCoord in alternatives)
            {
                TNodeType candidate = Graph[(int)altCoord.X, (int)altCoord.Y];
                if (!IsBlocked(candidate))
                {
                    destinationNode = candidate;
                    alternativeFound = true;
                    break;
                }
            }
            if (!alternativeFound)
            {
                return null;
            }
        }

        ConcurrentDictionary<TNodeType, int> gCost = new ConcurrentDictionary<TNodeType, int>();
        ConcurrentDictionary<TNodeType, TNodeType> cameFrom = new ConcurrentDictionary<TNodeType, TNodeType>();

        int HeuristicCost(TNodeType from, TNodeType to)
        {
            TCoordinate fromCoord = new TCoordinate();
            fromCoord.SetCoordinate(from.GetCoordinate());

            TCoordinate toCoord = new TCoordinate();
            toCoord.SetCoordinate(to.GetCoordinate());

            return Distance(fromCoord, toCoord);
        }

        foreach (TNodeType node in Graph)
        {
            gCost[node] = int.MaxValue;
        }
        
        gCost[startNode] = startNode.GetCost();

        PriorityQueue<TNodeType, int> openQueue = new PriorityQueue<TNodeType, int>();
        openQueue.Enqueue(startNode, gCost[startNode] + HeuristicCost(startNode, destinationNode));

        ConcurrentDictionary<TNodeType, bool> closedSet = new ConcurrentDictionary<TNodeType, bool>();

        while (openQueue.Count > 0)
        {
            TNodeType current = openQueue.Dequeue();

            if (NodesEquals(current, destinationNode))
            {
                return ReconstructPath(cameFrom, startNode, destinationNode);
            }

            if (!closedSet.TryAdd(current, true))
            {
                continue;
            }

            ICollection<TCoordinateType> neighbors = GetNeighbors(current);
            Parallel.ForEach(neighbors, neighbor =>
            {
                TNodeType neighborNode = Graph[(int)neighbor.X, (int)neighbor.Y];

                if (closedSet.ContainsKey(neighborNode))
                    return;

                int tentative_g = gCost[current] + MoveToNeighborCost(current, neighborNode);

                if (tentative_g < gCost[neighborNode])
                {
                    gCost[neighborNode] = tentative_g;
                    cameFrom[neighborNode] = current;

                    int fCost = tentative_g + HeuristicCost(neighborNode, destinationNode);
                    lock (openQueue)
                    {
                        openQueue.Enqueue(neighborNode, fCost);
                    }
                }
            });
        }

        return null;
    }

    /// <summary>
    /// Reconstructs the path from start to goal using the cameFrom data.
    /// </summary>
    private List<TNodeType> ReconstructPath(
        ConcurrentDictionary<TNodeType, TNodeType> cameFrom,
        TNodeType start,
        TNodeType goal)
    {
        List<TNodeType> path = new List<TNodeType>();
        TNodeType current = goal;

        while (!NodesEquals(current, start))
        {
            path.Add(current);
            if (!cameFrom.TryGetValue(current, out TNodeType parent))
            {
                break;
            }
            current = parent;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Given two node coordinates, compute an integer distance (e.g., Manhattan or Euclidean).
    /// </summary>
    protected abstract int Distance(TCoordinate a, TCoordinate b);

    /// <summary>
    /// Returns the valid neighbors (adjacent coordinates) for a given node.
    /// </summary>
    protected abstract ICollection<TCoordinateType> GetNeighbors(TNodeType node);

    /// <summary>
    /// Returns whether two TNodeType references represent the same node.
    /// </summary>
    protected abstract bool NodesEquals(TNodeType A, TNodeType B);

    /// <summary>
    /// Returns the cost of moving from node A to node B.
    /// </summary>
    protected abstract int MoveToNeighborCost(TNodeType A, TNodeType B);

    /// <summary>
    /// Returns true if the node is blocked (e.g., an obstacle or wall).
    /// </summary>
    protected abstract bool IsBlocked(TNodeType node);

    /// <summary>
    /// Given a coordinate for a blocked neighbor, returns alternative adjacent coordinates
    /// that might be traversable. (Implementation provided in your subclass.)
    /// </summary>
    protected abstract ICollection<TCoordinateType> GetAlternativeCoordinates(TCoordinateType blockedCoordinate);
}