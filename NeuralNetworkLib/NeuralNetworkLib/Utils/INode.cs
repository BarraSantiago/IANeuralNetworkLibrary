namespace NeuralNetworkLib.Utils
{
    public enum NodeType
    {
        Empty,
        Lake,
        Sand,
        Plains,
        Mountain
    }

    public enum NodeTerrain
    {
        Empty,
        Mine,
        Tree,
        Lake,
        Stump,
        TownCenter,
        Construction,
        WatchTower,
        Mountain
    }

    public interface INode
    {
        public bool IsBlocked();
        public void SetBlocked(bool blocked);
    }

    public interface INode<Coordinate> : IEquatable<Coordinate>
        where Coordinate : IEquatable<Coordinate>
    {
        public float X { get; set; }
        public float Y { get; set; }
        public void SetCoordinate(Coordinate coordinateType);
        public Coordinate GetCoordinate();
        public void SetNeighbors(ICollection<Coordinate> neighbors);
        public ICollection<Coordinate> GetNeighbors();
        public int GetCost();
        public bool IsOccupied { get; set; }
        public void SetCost(int newCost);
        NodeType NodeType { get; set; }
        NodeTerrain NodeTerrain { get; set; }
        int Resource { get; set; }
    }
}