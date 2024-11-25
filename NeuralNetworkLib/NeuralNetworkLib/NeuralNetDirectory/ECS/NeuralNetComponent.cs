using NeuralNetworkLib.NeuralNetDirectory.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.NeuralNetDirectory.ECS
{
    public class NeuralNetComponent : EcsComponent
    {
        public float[] Fitness;
        public float[] FitnessMod;
        public List<List<NeuronLayer>> Layers { get; set; } = new List<List<NeuronLayer>>();
    }
}