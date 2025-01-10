using NeuralNetworkLib.Utils;

public class SimNode<Coordinate> : INode, INode<Coordinate>, IEquatable<INode<Coordinate>>, IEquatable<SimNode<IVector>> 
    where Coordinate : IEquatable<Coordinate>
{
    public bool IsOccupied { get; set; }
    public NodeType NodeType { get; set; }
    public NodeTerrain NodeTerrain { get; set; }
    public int Resource { get; set; }

    private int cost;
    private Coordinate coordinate;
    private ICollection<INode<Coordinate>> neighbors;

    public SimNode()
    {
    }

    public SimNode(Coordinate coord)
    {
        coordinate = coord;
    }


    public bool Equals(INode<Coordinate> other)
    {
        return other != null && coordinate.Equals(other.GetCoordinate());
    }

    public bool IsBlocked()
    {
        return false;
    }

    public void SetCoordinate(Coordinate coordinate)
    {
        this.coordinate = coordinate;
    }

    public Coordinate GetCoordinate()
    {
        return coordinate;
    }

    public void SetNeighbors(ICollection<INode<Coordinate>> neighbors)
    {
        this.neighbors = neighbors;
    }

    public ICollection<INode<Coordinate>> GetNeighbors()
    {
        return neighbors;
    }

    public int GetCost()
    {
        return cost;
    }

    public void SetCost(int newCost)
    {
        cost = newCost;
    }

    public bool Equals(Coordinate other)
    {
        return coordinate.Equals(other);
    }

    public bool EqualsTo(INode<Coordinate> other)
    {
        return coordinate.Equals(other.GetCoordinate());
    }

    protected bool Equals(SimNode<Coordinate> other)
    {
        return coordinate.Equals(other.coordinate);
    }

    public bool Equals(SimNode<IVector> other)
    {
        return coordinate.Equals(other.GetCoordinate());
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SimNode<Coordinate>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<Coordinate>.Default.GetHashCode(coordinate);
    }
}