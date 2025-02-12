using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.ECS.NeuralNetECS
{
    public class NeuralNetComponent : EcsComponent
    {
        public float[] Fitness;
        public float[] FitnessMod;
        public NeuronLayer[][] Layers { get; set; }
    }
}