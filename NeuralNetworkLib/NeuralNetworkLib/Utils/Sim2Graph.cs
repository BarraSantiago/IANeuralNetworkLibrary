namespace NeuralNetworkLib.Utils
{
    public class Sim2Graph : SimGraph<SimNode<IVector>, SimCoordinate, IVector>
    {
        private readonly ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 32
        };

        public int MinX => 0;
        public int MaxX => CoordNodes.GetLength(0);
        public int MinY => 0;
        public int MaxY => CoordNodes.GetLength(1);

        public Sim2Graph(int x, int y, float cellSize) : base(x, y, cellSize)
        {
        }

        public override void CreateGraph(int x, int y, float cellSize)
        {
            CoordNodes = new SimCoordinate[x, y];

            Parallel.For(0, x, parallelOptions, i =>
            {
                for (int j = 0; j < y; j++)
                {
                    Random random = new Random();
                    int nodeTerrain = random.Next(0, 100);
                    int type = random.Next(0, 100);

                    SimCoordinate node = new SimCoordinate();
                    node.SetCoordinate(i * cellSize, j * cellSize);
                    CoordNodes[i, j] = node;

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * cellSize, j * cellSize));
                    NodeType type2 = GetNodeType(type);
                    nodeType.NodeType = type2;
                    
                    nodeType.NodeTerrain = type2 switch
                    {
                        NodeType.Lake => NodeTerrain.Lake,
                        NodeType.Mountain => NodeTerrain.Empty,
                        _ => GetTerrain(nodeTerrain)
                    };
                    
                    NodesType[i, j] = nodeType;
                }
            });
        }

        // TODO test this
        public void LoadGraph(int[] nodeTypes, int[] nodeTerrains)
        {
            Parallel.For(0, CoordNodes.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < CoordNodes.GetLength(1); j++)
                {
                    SimCoordinate node = new SimCoordinate();
                    node.SetCoordinate(i * CellSize, j * CellSize);
                    CoordNodes[i, j] = node;

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * CellSize, j * CellSize));
                    nodeType.NodeType = (NodeType)nodeTypes[i * CoordNodes.GetLength(1) + j];
                    nodeType.NodeTerrain = (NodeTerrain)nodeTerrains[i * CoordNodes.GetLength(1) + j];
                    NodesType[i, j] = nodeType;
                }
            });
        }
        
        private NodeType GetNodeType(int type)
        {
            return type switch
            {
                < 50 => NodeType.Plains,
                < 60 => NodeType.Mountain,
                < 85 => NodeType.Sand,
                < 100 => NodeType.Lake,
                _ => NodeType.Plains
            };
        }

        private NodeTerrain GetTerrain(int nodeTerrain)
        {
            return nodeTerrain switch
            {
                < 60 => NodeTerrain.Empty,
                < 80 => NodeTerrain.Mine,
                < 100 => NodeTerrain.Tree,
                _ => NodeTerrain.Empty
            };
        }

        public bool IsWithinGraphBorders(IVector position)
        {
            return position.X >= MinX && position.X <= MaxX - 1 &&
                   position.Y >= MinY && position.Y <= MaxY - 1;
        }
    }
}