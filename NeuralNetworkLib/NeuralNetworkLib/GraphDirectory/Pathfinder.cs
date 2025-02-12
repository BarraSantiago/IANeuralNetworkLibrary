using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.GraphDirectory;

public abstract class Pathfinder<TNodeType, TCoordinateType, TCoordinate>
    where TNodeType : INode<TCoordinateType>
    where TCoordinateType : IEquatable<TCoordinateType>, IVector
    where TCoordinate : ICoordinate<TCoordinateType>, new()
{
    protected TNodeType[,] Graph;

    public List<TNodeType> FindPath(TNodeType startNode, TNodeType destinationNode)
    {
        // Fast check for blocked destination with optimized alternative search
        if (IsBlocked(destinationNode))
        {
            foreach (TCoordinateType? altCoord in GetAlternativeCoordinates(destinationNode.GetCoordinate()))
            {
                TNodeType candidate = Graph[(int)altCoord.X, (int)altCoord.Y];
                if (!IsBlocked(candidate))
                {
                    destinationNode = candidate;
                    break;
                }
            }
            if (IsBlocked(destinationNode)) return null;
        }

        FastPriorityQueue<TNodeType> openSet = new FastPriorityQueue<TNodeType>();
        Dictionary<TNodeType, int> gScore = new Dictionary<TNodeType, int>();
        Dictionary<TNodeType, TNodeType> cameFrom = new Dictionary<TNodeType, TNodeType>();
        HashSet<TNodeType> closedSet = new HashSet<TNodeType>();

        // Precompute destination coordinates
        TCoordinate destCoord = new TCoordinate();
        destCoord.SetCoordinate(destinationNode.GetCoordinate());

        openSet.Enqueue(startNode, 0);
        gScore[startNode] = startNode.GetCost();

        while (openSet.Count > 0)
        {
            TNodeType current = openSet.Dequeue();

            if (NodesEquals(current, destinationNode))
            {
                return ReconstructPath(cameFrom, current);
            }

            if (!closedSet.Add(current)) continue;

            foreach (TCoordinateType? neighborCoord in GetNeighbors(current))
            {
                TNodeType neighbor = Graph[(int)neighborCoord.X, (int)neighborCoord.Y];
                if (closedSet.Contains(neighbor)) continue;

                int tentativeG = gScore[current] + MoveToNeighborCost(current, neighbor);

                if (tentativeG >= (gScore.TryGetValue(neighbor, out int existingG) ? existingG : int.MaxValue))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;

                int fCost = tentativeG + Heuristic(neighbor.GetCoordinate(), destCoord);
                if (!openSet.Contains(neighbor))
                {
                    openSet.Enqueue(neighbor, fCost);
                }
                else
                {
                    openSet.UpdatePriority(neighbor, fCost);
                }
            }
        }

        return null;
    }

// Optimized supporting methods
    private List<TNodeType> ReconstructPath(Dictionary<TNodeType, TNodeType> cameFrom, TNodeType current)
    {
        List<TNodeType> path = new List<TNodeType>();
        while (cameFrom.TryGetValue(current, out TNodeType? parent))
        {
            path.Add(current);
            current = parent;
        }

        path.Add(current);
        path.Reverse();
        return path;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Heuristic(TCoordinateType aCoord, TCoordinate b)
    {
        TCoordinate a = new TCoordinate();
        a.SetCoordinate(aCoord);
        return Distance(a, b);
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