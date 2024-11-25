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
                    SimCoordinate node = new SimCoordinate();
                    node.SetCoordinate(i * cellSize, j * cellSize);
                    CoordNodes[i, j] = node;

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * cellSize, j * cellSize));
                    NodesType[i, j] = nodeType;
                }
            });
        }

        public bool IsWithinGraphBorders(IVector position)
        {
            return position.X >= MinX && position.X <= MaxX - 1 &&
                   position.Y >= MinY && position.Y <= MaxY - 1;
        }
    }
}