using Newtonsoft.Json;

namespace NeuralNetworkLib.Utils
{
    public class Sim2Graph : SimGraph<SimNode<IVector>, CoordinateNode, IVector>
    {
        private readonly ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 32
        };

        public int MinX => 0;
        public int MaxX => CoordNodes.GetLength(0);
        public int MinY => 0;
        public int MaxY => CoordNodes.GetLength(1);
        public float CellSize2 => CellSize;

        public CoordinateNode MapSize => _mapSize;

        private CoordinateNode _mapSize = new CoordinateNode();

        private const int MaxTerrains = 20;
        private int mines = 0;
        private int trees = 0;
        private int lakes = 0;

        public Sim2Graph(int x, int y, float cellSize) : base(x, y, cellSize)
        {
        }

        public override void CreateGraph(int x, int y, float cellSize)
        {
            CoordNodes = new CoordinateNode[x, y];
            _mapSize.SetCoordinate(x, y);
            Parallel.For(0, x, parallelOptions, i =>
            {
                for (int j = 0; j < y; j++)
                {
                    Random random = new Random();
                    int nodeTerrain = random.Next(0, 100);
                    int type = random.Next(0, 100);

                    CoordinateNode node = new CoordinateNode();
                    node.SetCoordinate(i * cellSize, j * cellSize);
                    CoordNodes[i, j] = node;

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * cellSize, j * cellSize));
                    NodeType type2 = GetNodeType(type);
                    nodeType.NodeType = type2;

                    nodeType.NodeTerrain = type2 switch
                    {
                        NodeType.Lake => NodeTerrain.Lake,
                        NodeType.Plains => GetTerrain(nodeTerrain),
                        _ => NodeTerrain.Empty,
                    };

                    NodesType[i, j] = nodeType;
                }
            });
        }

        public void LoadGraph(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file does not exist.", filePath);
            }

            string json = File.ReadAllText(filePath);
            List<NodeData>? nodeData = JsonConvert.DeserializeObject<List<NodeData>>(json);

            if (nodeData == null)
            {
                throw new InvalidOperationException("Failed to deserialize the node data.");
            }

            int index = 0;
            Parallel.For(0, CoordNodes.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < CoordNodes.GetLength(1); j++)
                {
                    CoordinateNode node = new CoordinateNode();
                    node.SetCoordinate(i * CellSize, j * CellSize);
                    CoordNodes[i, j] = node;

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * CellSize, j * CellSize));
                    nodeType.NodeType = (NodeType)nodeData[index].NodeType;
                    nodeType.NodeTerrain = (NodeTerrain)nodeData[index].NodeTerrain;
                    NodesType[i, j] = nodeType;

                    index++;
                }
            });
        }

        public void LoadGraph(int[] nodeTypes, int[] nodeTerrains)
        {
            Parallel.For(0, CoordNodes.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < CoordNodes.GetLength(1); j++)
                {
                    CoordinateNode node = new CoordinateNode();
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

        public void SaveGraph(string filePath)
        {
            List<NodeData> nodeData = new List<NodeData>();

            Parallel.For(0, CoordNodes.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < CoordNodes.GetLength(1); j++)
                {
                    int nodeType = (int)NodesType[i, j].NodeType;
                    int nodeTerrain = (int)NodesType[i, j].NodeTerrain;
                    nodeData.Add(new NodeData { NodeType = nodeType, NodeTerrain = nodeTerrain });
                }
            });

            string json = JsonConvert.SerializeObject(nodeData, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public class NodeData
        {
            public int NodeType { get; set; }
            public int NodeTerrain { get; set; }
        }

        private NodeType GetNodeType(int type)
        {
            NodeType nodeType = type switch
            {
                < 50 => NodeType.Plains,
                < 60 => NodeType.Mountain,
                < 85 => NodeType.Sand,
                < 100 => NodeType.Lake,
                _ => NodeType.Plains
            };
            if (nodeType != NodeType.Lake) return nodeType;

            lakes++;
            if (lakes > MaxTerrains) nodeType = NodeType.Plains;
            return nodeType;
        }

        private NodeTerrain GetTerrain(int nodeTerrain)
        {
            NodeTerrain terrain = nodeTerrain switch
            {
                < 60 => NodeTerrain.Empty,
                < 80 => NodeTerrain.Mine,
                < 100 => NodeTerrain.Tree,
                _ => NodeTerrain.Empty
            };

            switch (terrain)
            {
                case NodeTerrain.Mine:
                    mines++;
                    if (mines > MaxTerrains) terrain = NodeTerrain.Empty;
                    break;
                case NodeTerrain.Tree:
                    trees++;
                    if (trees > MaxTerrains) terrain = NodeTerrain.Empty;
                    break;
                default:
                    break;
            }

            return terrain;
        }

        public bool IsWithinGraphBorders(IVector position)
        {
            return position.X >= MinX && position.X <= MaxX - 1 &&
                   position.Y >= MinY && position.Y <= MaxY - 1;
        }
    }
}