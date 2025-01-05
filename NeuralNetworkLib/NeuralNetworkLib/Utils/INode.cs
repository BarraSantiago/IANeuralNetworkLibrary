namespace NeuralNetworkLib.Utils
{
    public enum NodeType
    {
        Empty,
        Blocked,
        Bush,
        Corpse,
        Carrion,
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
        Stump,
        TownCenter,
        WatchTower,
    }

    public interface INode
    {
        public bool IsBlocked();
    }

    public interface INode<Coordinate> : IEquatable<Coordinate>
        where Coordinate : IEquatable<Coordinate>
    {
        public void SetCoordinate(Coordinate coordinateType);

        public Coordinate GetCoordinate();

        public void SetNeighbors(ICollection<INode<Coordinate>> neighbors);

        public ICollection<INode<Coordinate>> GetNeighbors();

        public int GetCost();

        public void SetCost(int newCost);
        NodeType NodeType { get; set; }

        NodeTerrain NodeTerrain { get; set; }
        int Food { get; set; }
    }
}