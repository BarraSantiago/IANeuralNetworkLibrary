using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.GraphDirectory;

public class AStarPathfinder<TNodeType, TCoordinateType, TCoordinate> : Pathfinder<TNodeType, TCoordinateType, TCoordinate>
    where TNodeType : INode, INode<TCoordinateType>, new()
    where TCoordinateType : IEquatable<TCoordinateType>, IVector
    where TCoordinate : ICoordinate<TCoordinateType>, new()
{
    public AStarPathfinder(TNodeType[,] graph)
    {
        this.Graph = graph;
    }
    
    public void UpdateNode(TCoordinateType nodeCoord, int newCost, bool blocked)
    {
        TNodeType node = Graph[(int)nodeCoord.X, (int)nodeCoord.Y];
        node.SetCost(newCost);
        node.SetBlocked(blocked);
        
    }

    protected override int Distance(TCoordinate A, TCoordinate B)
    {
        if (A == null || B == null)
        {
            return int.MaxValue;
        }

        float distance = 0;
        distance += Math.Abs(A.GetX() - B.GetX());
        distance += Math.Abs(A.GetY() - B.GetY());

        return (int)distance;
    }

    protected override ICollection<TCoordinateType> GetNeighbors(TNodeType node)
    {
        return node.GetNeighbors();
    }

    protected override bool IsBlocked(TNodeType node)
    {
        return node.IsBlocked();
    }

    /// <summary>
    /// When a node is blocked, returns a collection of alternative coordinates adjacent
    /// to the blocked coordinate that may provide a viable detour. This implementation considers
    /// only the four cardinal directions (vertical and horizontal) and excludes diagonal alternatives.
    /// </summary>
    /// <param name="blockedCoordinate">The coordinate of the blocked neighbor.</param>
    /// <returns>A collection of alternative neighbor coordinates.</returns>
    protected override ICollection<TCoordinateType> GetAlternativeCoordinates(TCoordinateType blockedCoordinate)
    {
        List<TCoordinateType> alternatives = new List<TCoordinateType>();

        int width = Graph.GetLength(0);
        int height = Graph.GetLength(1);

        int x = Convert.ToInt32(blockedCoordinate.X);
        int y = Convert.ToInt32(blockedCoordinate.Y);

        int[] dx = { -1, 0, 1, 0 };
        int[] dy = { 0, -1, 0, 1 };

        for (int i = 0; i < dx.Length; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            if (newX < 0 || newY < 0 || newX >= width || newY >= height)
                continue;

            TCoordinateType altCoord = CreateCoordinate(newX, newY);
            alternatives.Add(altCoord);
        }

        return alternatives;
    }

    /// <summary>
    /// Helper method that attempts to create a new instance of CoordinateType given integer x and y.
    /// This implementation uses reflection via Activator.CreateInstance. Make sure that your CoordinateType
    /// provides a constructor with two parameters (x and y).
    /// </summary>
    /// <param name="x">The x-coordinate (typically an integer).</param>
    /// <param name="y">The y-coordinate (typically an integer).</param>
    /// <returns>A new instance of CoordinateType.</returns>
    protected virtual TCoordinateType CreateCoordinate(int x, int y)
    {
        return Graph[x,y].GetCoordinate();
    }

    protected override int MoveToNeighborCost(TNodeType A, TNodeType B)
    {
        if (!GetNeighbors(A).Contains(B.GetCoordinate()))
        {
            throw new InvalidOperationException("B node has to be a neighbor.");
        }

        return B.GetCost() + A.GetCost();
    }

    protected override bool NodesEquals(TNodeType A, TNodeType B)
    {
        if (A == null || B == null || A.GetCoordinate() == null || B.GetCoordinate() == null)
        {
            return false;
        }

        return A.GetCoordinate().Equals(B.GetCoordinate());
    }
}