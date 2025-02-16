using NeuralNetworkLib.DataManagement;

namespace NeuralNetworkLib.Utils;

public class SimNode<Coordinate> : INode, INode<Coordinate>, IEquatable<INode<Coordinate>>
    where Coordinate : IEquatable<Coordinate>
{
    public bool isBlocked = false;
    public NodeType NodeType { get; set; }

    public NodeTerrain NodeTerrain
    {
        get => _nodeTerrain;
        set
        {
            _nodeTerrain = value;

            switch (_nodeTerrain)
            {
                case NodeTerrain.Mine:
                case NodeTerrain.Tree:
                case NodeTerrain.Lake:
                    _resource = 20;
                    break;
                case NodeTerrain.Stump:
                    _resource = 1;
                    break;
                case NodeTerrain.TownCenter:
                case NodeTerrain.Construction:
                case NodeTerrain.Empty:
                case NodeTerrain.WatchTower:
                default:
                    break;
            }
        }
        
    }

    public int Resource
    {
        get => _resource;
        set
        {
            if (value <= _resource && value <= 0)
            {
                NodeTerrain terrain = NodeTerrain;
                NodeTerrain = _nodeTerrain == NodeTerrain.Tree ? NodeTerrain.Stump : NodeTerrain.Empty;
                DataContainer.OnUpdateVoronoi?.Invoke(terrain == NodeTerrain.WatchTower
                    ? NodeTerrain.TownCenter : terrain);
                DataContainer.OnUpdateVoronoi?.Invoke(NodeTerrain);
            }

            _resource = value;
        }
    }

    private int cost;
    private Coordinate coordinate;
    private ICollection<Coordinate> neighbors;
    private int _resource;
    private NodeTerrain _nodeTerrain;
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
        return isBlocked;
    }

    public float X { get; set; }
    public float Y { get; set; }

    public void SetCoordinate(Coordinate coordinate)
    {
        this.coordinate = coordinate;
    }

    public Coordinate GetCoordinate()
    {
        return coordinate;
    }

    public void SetNeighbors(ICollection<Coordinate> neighbors)
    {
        this.neighbors = neighbors;
    }

    public ICollection<Coordinate> GetNeighbors()
    {
        return neighbors;
    }


    public int GetCost()
    {
        return cost;
    }

    public bool IsOccupied { get; set; }

    public void SetCost(int newCost)
    {
        cost = newCost;
    }

    public void BuildWatchTower()
    {
        Resource++;
        if (Resource >= 100)
        {
            NodeTerrain = NodeTerrain.WatchTower;
        }
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

    public IVector? GetAdjacentNode()
    {
        foreach (IVector neighbor in neighbors)
        {
            SimNode<IVector> neighNode = DataContainer.GetNode(neighbor);
            if (!neighNode.IsOccupied) return neighbor;
        }

        return null;
    }

    public override int GetHashCode()
    {
        return EqualityComparer<Coordinate>.Default.GetHashCode(coordinate);
    }
}