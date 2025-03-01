using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;
using Random = System.Random;

namespace Pathfinder.Graph
{
    public class GraphManager<TVector, TTransform>
        where TTransform : ITransform<TVector> 
        where TVector : IVector, IEquatable<TVector>
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private Random random;

        public GraphManager(int width, int height)
        {
            Width = width;
            Height = height;
            random = new Random();
        }

        public SimNode<IVector> GetRandomPositionInLowerQuarter()
        {
            int x = random.Next(0, Width);
            int y = random.Next(1, Height / 4);
            return DataContainer.Graph.NodesType[x, y];
        }

        public SimNode<IVector> GetRandomPositionInUpperQuarter()
        {
            int x = random.Next(0, Width);
            int y = random.Next(3 * Height / 4, Height-1);
            return DataContainer.Graph.NodesType[x, y];
        }

        public SimNode<IVector> GetRandomPosition()
        {
            int x = random.Next(0, Width);
            int y = random.Next(0, Height);
            return DataContainer.Graph.NodesType[x, y];
        }
    }
}