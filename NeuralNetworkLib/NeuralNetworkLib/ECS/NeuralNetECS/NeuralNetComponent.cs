using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.ECS.NeuralNetECS
{
    public class NeuralNetComponent : EcsComponent
    {
        public float[] Fitness;
        public float[] FitnessMod;
        public List<List<NeuronLayer>> Layers { get; set; } = new List<List<NeuronLayer>>();
    }
}