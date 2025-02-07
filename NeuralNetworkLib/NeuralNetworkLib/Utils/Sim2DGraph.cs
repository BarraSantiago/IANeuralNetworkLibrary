using Newtonsoft.Json;

namespace NeuralNetworkLib.Utils
{
    public class Sim2DGraph : SimGraph<SimNode<IVector>, CoordinateNode, IVector>
    {
        private readonly ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 32
        };

        public int MinX => 0;
        public int MaxX => NodesType.GetLength(0);
        public int MinY => 0;
        public int MaxY => NodesType.GetLength(1);
        public float CellSize2 => CellSize;

        public CoordinateNode MapSize => _mapSize;

        private CoordinateNode _mapSize = new CoordinateNode();

        // TODO MODIFY THIS TO 20
        private const int MaxTerrains = 20;
        private int mines = 0;
        private int trees = 0;
        private int lakes = 0;

        public Sim2DGraph(int x, int y, float cellSize) : base(x, y, cellSize)
        {
        }

        public override void CreateGraph(int x, int y, float cellSize)
        {
            _mapSize.SetCoordinate(x, y);
            Parallel.For(0, x, parallelOptions, i =>
            {
                for (int j = 0; j < y; j++)
                {
                    Random random = new Random();
                    double type = random.NextDouble();

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * cellSize, j * cellSize));
                    nodeType.X = i * cellSize;
                    nodeType.Y = j * cellSize;
                    nodeType.NodeType = GetNodeType(type);
                    if(nodeType.NodeType == NodeType.Lake) nodeType.NodeTerrain = NodeTerrain.Lake;
                    NodesType[i, j] = nodeType;
                }
            });
            AssignRandomTerrains();
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
            Parallel.For(0, NodesType.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < NodesType.GetLength(1); j++)
                {
                    NodesType[i, j].SetCoordinate(new MyVector(i * CellSize, j * CellSize));
                    NodesType[i, j].NodeType = (NodeType)nodeData[index].NodeType;
                    NodesType[i, j].NodeTerrain = (NodeTerrain)nodeData[index].NodeTerrain;

                    index++;
                }
            });
        }

        public void LoadGraph(int[] nodeTypes, int[] nodeTerrains)
        {
            Parallel.For(0, NodesType.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < NodesType.GetLength(1); j++)
                {
                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * CellSize, j * CellSize));
                    nodeType.NodeType = (NodeType)nodeTypes[i * NodesType.GetLength(1) + j];
                    nodeType.NodeTerrain = (NodeTerrain)nodeTerrains[i * NodesType.GetLength(1) + j];
                    NodesType[i, j] = nodeType;
                }
            });
        }

        public void SaveGraph(string filePath)
        {
            List<NodeData> nodeData = new List<NodeData>();

            Parallel.For(0, NodesType.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < NodesType.GetLength(1); j++)
                {
                    int nodeType = (int)NodesType[i, j].NodeType;
                    int nodeTerrain = (int)NodesType[i, j].NodeTerrain;
                    nodeData.Add(new NodeData { NodeType = nodeType, NodeTerrain = nodeTerrain });
                }
            });

            string json = JsonConvert.SerializeObject(nodeData, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public void AssignRandomTerrains()
        {
            List<SimNode<IVector>> allNodes = new List<SimNode<IVector>>();

            for (int i = 0; i < NodesType.GetLength(0); i++)
            {
                for (int j = 0; j < NodesType.GetLength(1); j++)
                {
                    allNodes.Add(NodesType[i, j]);
                }
            }

            Random random = new Random();
            allNodes = allNodes.OrderBy(x => random.Next()).ToList();

            Parallel.For(0, 2 * MaxTerrains, parallelOptions, i =>
            {
                if (i < MaxTerrains && i < allNodes.Count)
                {
                    allNodes[i].NodeTerrain = NodeTerrain.Mine;
                }
                else if (i < 2 * MaxTerrains && i < allNodes.Count)
                {
                    allNodes[i].NodeTerrain = NodeTerrain.Tree;
                }
            });
        }

        public class NodeData
        {
            public int NodeType { get; set; }
            public int NodeTerrain { get; set; }
        }

        private NodeType GetNodeType(double type)
        {
            NodeType nodeType = type switch
            {
                < 0.98 => NodeType.Plains,
                < 0.985 => NodeType.Mountain,
                < 0.997 => NodeType.Sand,
                < 1 => NodeType.Lake,
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