namespace NeuralNetworkLib.Utils
{
    public abstract class SimGraph<TNodeType, TCoordinateNode, TCoordinateType>
        where TNodeType : INode<TCoordinateType>
        where TCoordinateNode : ICoordinate<TCoordinateType>, new()
        where TCoordinateType : IEquatable<TCoordinateType>
    {
        public static TCoordinateNode MapDimensions;
        public static TCoordinateNode OriginPosition;
        public static float CellSize;
        public readonly TNodeType[,] NodesType;

        private ParallelOptions parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 32
        };

        public SimGraph(int x, int y, float cellSize)
        {
            MapDimensions = new TCoordinateNode();
            MapDimensions.SetCoordinate(x, y);
            CellSize = cellSize;

            NodesType = new TNodeType[x, y];

            CreateGraph(x, y, cellSize);

            AddNeighbors(cellSize);
        }

        public abstract void CreateGraph(int x, int y, float cellSize);

        private void AddNeighbors(float cellSize)
        {
            Parallel.For((long)0, NodesType.GetLength(0), parallelOptions, i =>
            {
                for (int j = 0; j < NodesType.GetLength(1); j++)
                {
                    List<TCoordinateType> neighbors = new List<TCoordinateType>();

                    for (int k = 0; k < NodesType.GetLength(0); k++)
                    {
                        for (int l = 0; l < NodesType.GetLength(1); l++)
                        {
                            if (i == k && j == l) continue;

                            bool isNeighbor =
                                (Approximately(NodesType[i, j].X, NodesType[k, l].X) &&
                                 Approximately(Math.Abs(NodesType[i, j].Y - NodesType[k, l].Y), cellSize)) ||
                                (Approximately(NodesType[i, j].Y, NodesType[k, l].Y) &&
                                 Approximately(Math.Abs(NodesType[i, j].X - NodesType[k, l].X), cellSize));
        
                            if (isNeighbor) neighbors.Add(NodesType[k, l].GetCoordinate());
                        }
                    }

                    NodesType[i, j].SetNeighbors(neighbors);
                }
            });
        }

        public bool Approximately(float a, float b)
        {
            return Math.Abs(a - b) < 1e-6f;
        }
    }
}